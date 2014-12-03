using System;
using BridgeChat.PluginBase;
using System.Security.Cryptography;
using System.Net;

namespace BridgeChat.ConsoleClient
{
    public class ConsoleModule : Module
    {
        public ConsoleModule(string server, int port)
            : base("Console client", "CON", server, port, supportsPlaintext: true)
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

