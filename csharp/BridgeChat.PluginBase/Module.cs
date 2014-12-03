using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using BridgeChat.Protocol;
using ProtoBuf;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace BridgeChat.PluginBase
{
    public abstract class Module
    {
        private readonly TcpClient Client = new TcpClient();
        private readonly ConcurrentQueue<byte[]> SendQ = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim SendQSem = new SemaphoreSlim(0);
        private readonly string Server;
        private readonly int Port;
        private readonly string LongName, ShortName;
        private readonly ConcurrentDictionary<uint, Group> Groups = new ConcurrentDictionary<uint, Group>();
        private bool ShuttingDown;

        private readonly Protocol.MessageFormat[] MandatoryFormats, OptionalFormats;

        public Module(string longName, string shortName, string server, int port,
            Protocol.MessageFormat[] mandatoryFormats, Protocol.MessageFormat[] optionalFormats = null)
        {
            LongName = longName;
            ShortName = shortName;
            Server = server;
            Port = port;

            MandatoryFormats = mandatoryFormats ?? new MessageFormat[0];
            OptionalFormats = optionalFormats ?? new MessageFormat[0];
        }

        public abstract bool TryBindGroup(uint groupid, string parameter, out Group result, out string diagnostics);

        internal void QueueMessage<T>(T t)
        {
            SendQ.Enqueue(Util.ProtobufSerialize(t));
            SendQSem.Release();
        }

        public virtual void Shutdown()
        {
            ShuttingDown = true;
            Client.Close();
        }

        public virtual async Task Run()
        {
            Debug.WriteLine("connecting...");
            await Client.ConnectAsync(Server, Port);
            Debug.WriteLine("connected!");
            using (var netstream = Client.GetStream()) {
                var moduleIntro = new Protocol.ModuleIntro {
                    LongName = LongName,
                    ShortName = ShortName,
                };
                moduleIntro.MandatoryFormats.AddRange(MandatoryFormats);
                moduleIntro.MandatoryFormats.AddRange(OptionalFormats);
                var intro = Protocol.Util.ProtobufSerialize(moduleIntro);
                await netstream.WriteAsync(intro, 0, intro.Length);

                // main loop:
                Func<Task<Protocol.GroupMessage>> mkProtoRead = () => Protocol.Util.AsyncProtobufRead<Protocol.GroupMessage>(netstream);
                Func<Task> mkSendQWait = SendQSem.WaitAsync;

                var protoRead = mkProtoRead();
                var sendQWait = mkSendQWait();
                Task sendReady = Task.FromResult<object>(null);

                while (true) {
                    var finished = await Task.WhenAny(protoRead, Task.WhenAll(sendQWait, sendReady));
                    if (finished == protoRead) {
                        GroupMessage msg;
                        try {
                            msg = await protoRead;
                        } catch (IOException) {
                            if (ShuttingDown) break;
                            else throw;
                        }

                        if (msg == null) // socket closed, shutting down
                            break;

                        Group group;
                        if (msg.BindingRequest != null) {
                            var br = msg.BindingRequest;
                            var response = new BindingResponse();
                            string diagnostic;
                            response.Success = TryBindGroup(msg.GroupId, br.BindInfo,
                                out group, out diagnostic);
                            response.Diagnostic = diagnostic;

                            if (response.Success && !Groups.TryAdd(group.Id, group))
                                throw new Exception("impossible");

                            QueueMessage(new GroupMessage {
                                GroupId = group.Id,
                                BindingResponse = response
                            });
                        } else {
                            group = Groups[msg.GroupId];
                            if (msg.GroupStatusChange != null) {
                                var gsc = msg.GroupStatusChange;
                                if (gsc.TopicSpecified)
                                    group.HandleTopicChange(gsc.Topic);
                            } else if (msg.UserEvent != null) {
                                var ue = msg.UserEvent;
                                if (!ue.PluginIdSpecified)
                                    throw new ProtocolViolationException();

                                if (ue.ChatMessage != null)
                                    group.HandleMessage(ue.PluginId, ue.Username, ue.ChatMessage);

                                if (ue.UserStatus != null) {
                                    var us = ue.UserStatus;
                                    if (us.OnlineStatusSpecified) {
                                        if (us.OnlineStatus)
                                            // online status changed to true --> user joined
                                            group.AddUser(ue.PluginId, ue.Username);
                                        else
                                            // online status changed to false --> user left
                                            group.RemoveUser(ue.PluginId, ue.Username);
                                    }
                                }
                            } else if (msg.UnbindRequest != null) {
                                group.Unbind();
                                Group gout;
                                if ((!Groups.TryRemove(group.Id, out gout)) || (gout != group))
                                    throw new Exception("impossible");
                            } else
                                throw new NotImplementedException("They sent this thing but we have no handler for it: " + msg);
                        }

                        // read another one
                        protoRead = mkProtoRead();
                    } else {
                        // sendQWait AND sendReady
                        // aka: We're ready to send data AND there's data we should send

                        // trigger exceptions, if any
                        await sendQWait;
                        await sendReady;

                        byte[] packet;
                        if (!SendQ.TryDequeue(out packet))
                            throw new Exception("impossible");

                        sendReady = netstream.WriteAsync(packet, 0, packet.Length);
                        sendQWait = mkSendQWait();
                    }
                }

                Debug.WriteLine("network loop down");
            }
        }
    }
}

