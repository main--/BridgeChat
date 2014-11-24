using System;
using ProtoBuf;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;

namespace BridgeChat.Core
{
    public class DummyDatastore : IDatastore
    {
        private List<Group> Groups = new List<Group>();
        private ISet<Binding> Bindings = new HashSet<Binding>();
        private uint nextid = 1;

        public DummyDatastore()
        {
            var grp = CreateGroup();
            Bindings.Add(new Binding { GroupId = grp.Id, Module = "XMPP", BindParams = "asdf" });
            Bindings.Add(new Binding { GroupId = grp.Id, Module = "CON", BindParams = "ignored" });
        }

        public Group CreateGroup()
        {
            var group = new Group(grp => {
            }, grp => {
                Groups.Remove(grp);
                RemovalHandler(grp);
            }, bind => Bindings.Add(bind), bind => Bindings.Remove(bind), nextid++);
            Groups.Add(group);
            return group;
        }
            
        public Action<Group> RemovalHandler { get; set; }
        public System.Collections.Generic.IEnumerable<Group> SavedGroups {
            get {
                return Groups;
            }
        }
        public System.Collections.Generic.IEnumerable<Binding> SavedBindings {
            get {
                return Bindings;
            }
        }
    }

    public class MainClass
    {
        public static void Main(string[] args)
        {
            var server = new ChatServer(new IPEndPoint(IPAddress.Loopback, 31337), new DummyDatastore());
            var runTask = server.Run();
            Console.ReadKey(true);
            server.StopListening();
            runTask.Wait();
        }
    }
}
