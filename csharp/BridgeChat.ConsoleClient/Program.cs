using System;
using BridgeChat.PluginBase;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace BridgeChat.ConsoleClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            #if DEBUG
            if (!Debugger.IsAttached)
                Trace.Listeners.Add(new ConsoleTraceListener(true));
            #endif

            var mod = new ConsoleModule("localhost", 31337);
            Console.CancelKeyPress += (sender, e) => { mod.Shutdown(); e.Cancel = true; };
            mod.Run().Wait();
            Environment.Exit(0);
        }
    }
}
