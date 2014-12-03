using System;
using System.Security.Cryptography;
using System.Net;

using BridgeChat.PluginBase;

namespace BridgeChat.ConsoleClient
{
    public class ConsoleModule : Module
    {
        public ConsoleModule(string server, int port)
            : base("Console client", "CON", server, port, new Protocol.MessageFormat[] { Protocol.MessageFormat.Plaintext })
        {
        }

        public override bool TryBindGroup(uint groupid, string parameter, out Group result, out string diagnostics)
        {
            Console.WriteLine("---- bound ----");
            result = new ConsoleGroup(this, groupid);
            diagnostics = null;
            return true;
        }
    }
}

