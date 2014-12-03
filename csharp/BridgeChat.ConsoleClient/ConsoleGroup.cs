using System;
using BridgeChat.PluginBase;
using System.Threading;
using System.Linq.Expressions;

namespace BridgeChat.ConsoleClient
{
    public class ConsoleGroup : Group
    {
        public ConsoleGroup(ConsoleModule mod, uint id)
            : base(mod, id)
        {
            new Thread(() => {
                string name;
                MyUsers.Add(name = Console.ReadLine());
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line == "quit") {
                        MyUsers.Remove(name);
                        break;
                    }
                    SendMessage(name, new Protocol.ChatMessage { Plaintext = line });
                }
            }).Start();
        }

        public override void HandleTopicChange(string topic)
        {
            Console.WriteLine("Topic has been set to: {0}", topic);
        }
        public override void HandleMessage(string module, string username, Protocol.ChatMessage message)
        {
            Console.WriteLine("[{0}] {1}: {2}", module, username, message.Plaintext);
        }
        public override void AddUser(string module, string username)
        {
            Console.WriteLine("[{0}] {1} joined", module, username);
        }
        public override void RemoveUser(string module, string username)
        {
            Console.WriteLine("[{0}] {1} left", module, username);
        }
        public override void Unbind()
        {
            Console.WriteLine("---- unbound ----");
        }
    }
}

