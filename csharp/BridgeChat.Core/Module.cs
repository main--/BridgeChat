using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using BridgeChat.ConversionFramework;

namespace BridgeChat.Core
{
    public class Module
    {
        public static readonly Module ServerManagementModule = new Module("The BridgeChat server", "Server", null, null);

        public string LongName { get; private set; }
        public string ShortName { get; private set; }
        public ConcurrentQueue<byte[]> SendQ { get; private set; }
        public SemaphoreSlim SendQSem { get; private set; }
        public Type[] MandatoryMessageTypes { get; private set; }
        public Type[] OptionalMessageTypes { get; private set; }

        public Module(string longname, string shortname, Type[] mandatoryMessageTypes, Type[] optionalMessageTypes)
        {
            LongName = longname;
            ShortName = shortname;
            SendQ = new ConcurrentQueue<byte[]>();
            SendQSem = new SemaphoreSlim(0);
            MandatoryMessageTypes = mandatoryMessageTypes;
            OptionalMessageTypes = optionalMessageTypes;
        }

        public void QueueMessage<T>(T msg)
        {
            SendQ.Enqueue(Protocol.Util.ProtobufSerialize(msg));
            SendQSem.Release();
        }

        public void BindGroup(uint group, string parameter)
        {
            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                BindingRequest = new Protocol.BindingRequest { BindInfo = parameter }
            });
        }

        public void UnbindGroup(uint group)
        {
            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                UnbindRequest = new Protocol.UnbindRequest { }
            });
        }

        public void SetTopic(uint group, string topic)
        {
            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                GroupStatusChange = new Protocol.GroupStatus { Topic = topic }
            });
        }

        public void SendChatMessage(uint group, Module module, string username, Protocol.ChatMessage message)
        {
            // Run the conversion. TODO: Run a single conversion for all modules
            message = MessageFormatUtil.PostprocessAfterConversionFramework(
                ConversionManager.Instance.RunConversion(message.PrepareForConversionFramework().ToArray(),
                    MandatoryMessageTypes, OptionalMessageTypes));

            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                UserEvent = new BridgeChat.Protocol.UserEvent {
                    PluginId = module.ShortName,
                    Username = username,
                    ChatMessage = message
                }
            });
        }

        public void AddUser(uint group, Module module, string name)
        {
            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                UserEvent = new BridgeChat.Protocol.UserEvent {
                    PluginId = module.ShortName,
                    Username = name,
                    UserStatus = new BridgeChat.Protocol.UserStatus { OnlineStatus = true }
                }
            });
        }

        public void RemoveUser(uint group, Module module, string name)
        {
            QueueMessage(new Protocol.GroupMessage {
                GroupId = group,
                UserEvent = new BridgeChat.Protocol.UserEvent {
                    PluginId = module.ShortName,
                    Username = name,
                    UserStatus = new BridgeChat.Protocol.UserStatus { OnlineStatus = false }
                }
            });
        }

        public async Task NetworkLoop(NetworkStream netstream, IDictionary<uint, Group> groups)
        {
            Func<Task<Protocol.GroupMessage>> mkProtoRead = () => Protocol.Util.AsyncProtobufRead<Protocol.GroupMessage>(netstream);
            Func<Task> mkSendQWait = SendQSem.WaitAsync;

            var protoRead = mkProtoRead();
            var sendQWait = mkSendQWait();
            Task sendReady = Task.FromResult<object>(null);

            while (true) {
                var finished = await Task.WhenAny(protoRead, Task.WhenAll(sendQWait, sendReady));
                if (finished == protoRead) {
                    var msg = await protoRead;
                    if (msg == null)
                        break; // disconnect

                    var group = groups[msg.GroupId];
                    if (msg.BindingResponse != null) {
                        string bindmsg;
                        var br = msg.BindingResponse;
                        if (br.Success) {
                            group.AddBinding(this);
                            bindmsg = "Module '{0}' successfully bound";
                        } else
                            bindmsg = "Failed to bind module '{0}'";
                        bindmsg = String.Format(bindmsg, LongName);
                        if (br.DiagnosticSpecified)
                            bindmsg += ": " + br.Diagnostic;
                        group.SendMessage(ServerManagementModule, "Module manager", new Protocol.ChatMessage { Plaintext = bindmsg });
                    } else if (msg.GroupStatusChange != null) {
                        var gsc = msg.GroupStatusChange;
                        if (gsc.TopicSpecified)
                            group.SetTopic(gsc.Topic, this);
                    } else if (msg.UserEvent != null) {
                        var ue = msg.UserEvent;
                        if (ue.PluginIdSpecified)
                            throw new ProtocolViolationException();

                        if (ue.ChatMessage != null)
                            group.SendMessage(this, ue.Username, ue.ChatMessage);

                        if (ue.UserStatus != null) {
                            var us = ue.UserStatus;
                            if (us.OnlineStatusSpecified) {
                                if (us.OnlineStatus)
                                    // online status changed to true --> user joined
                                    group.AddUser(this, ue.Username);
                                else
                                    // online status changed to false --> user left
                                    group.RemoveUser(this, ue.Username);
                            }
                        }
                    } else throw new NotImplementedException("They sent this thing but we have no handler for it: " + msg);

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
        }
    }
}

