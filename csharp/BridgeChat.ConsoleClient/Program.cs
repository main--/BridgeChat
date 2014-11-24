using System;
using BridgeChat.PluginBase;
using System.Net;
using System.Threading;

namespace BridgeChat.ConsoleClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var mod = new ConsoleModule("localhost", 31337);
            Thread.Sleep(1000);
            Console.CancelKeyPress += (sender, e) => { mod.Shutdown(); e.Cancel = true; };
            mod.Run().Wait();
            Environment.Exit(0);
        }
    }
}
