using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using BridgeChat.Protocol;
using System.Threading;
using System.Linq;

namespace BridgeChat.Core
{
    public class ChatServer
    {
        private readonly IList<Module> Modules = new CopyOnWriteArrayList<Module>();
        private readonly IDictionary<uint, Group> Groups = new ConcurrentDictionary<uint, Group>();

        private readonly TcpListener Listener;
        private readonly IDatastore Datastore;

        public ChatServer(IPEndPoint endpoint, IDatastore datastore)
        {
            Datastore = datastore;
            Listener = new TcpListener(endpoint);
        }

        public void CreateNewGroup()
        {
            var grp = Datastore.CreateGroup();
            Groups.Add(grp.Id, grp);
        }

        public async Task Run()
        {
            // begin: init groups
            foreach (var group in Datastore.SavedGroups)
                Groups.Add(group.Id, group);

            Listener.Start();

            var tasks = new List<Task>();
            tasks.Add(Listener.AcceptTcpClientAsync());
            while (tasks.Count > 0)
            {
                var finished = await Task.WhenAny(tasks);
                tasks.Remove(finished);

                var acceptTask = finished as Task<TcpClient>;
                if (acceptTask != null) {
                    TcpClient client;
                    try {
                        client = await acceptTask;
                    } catch (ObjectDisposedException) {
                        // shutting down
                        client = null;
                    }

                    if (client != null) {
                        tasks.Add(HandleClient(client));
                        // accept another one
                        tasks.Add(Listener.AcceptTcpClientAsync());
                    }
                } else try {
                    await finished; // unwrap exceptions
                } catch (Exception e) {
                    Trace.WriteLine(e, "Error while handling client");
                }
            }
        }

        public void StopListening()
        {
            Listener.Stop();
        }

        public async Task HandleClient(TcpClient client)
        {
            using (var netstream = client.GetStream()) {
                var intro = await Util.AsyncProtobufRead<ModuleIntro>(netstream);
                var self = new Module(intro.LongName, intro.ShortName,
                    intro.MandatoryFormats.Select(MessageFormatUtil.ToType).ToArray(),
                    intro.OptionalFormats.Select(MessageFormatUtil.ToType).ToArray());
                Modules.Add(self);
                Trace.WriteLine(intro.LongName, "Module online");

                var netloop = self.NetworkLoop(netstream, Groups);

                // restore bindings
                foreach (var binding in Datastore.SavedBindings.ToArray().Where(bdg => bdg.Module == self.ShortName))
                    Groups[binding.GroupId].RequestBinding(self, binding.BindParams);
                
                try {
                    await netloop;
                } finally {
                    // unbind all
                    foreach (var binding in Datastore.SavedBindings.ToArray().Where(bdg => bdg.Module == self.ShortName))
                        Groups[binding.GroupId].UnloadBinding(self);
                }
            }
        }
    }
}

