using System;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace BridgeChat.XMPP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            #if DEBUG
            if (!Debugger.IsAttached)
                Trace.Listeners.Add(new ConsoleTraceListener(true));
            #endif

            var module = new XMPPModule("localhost", 3127, "secret", "test.ehvag.de", "localhost", 31337);
            var runTask = module.Run();
            Console.CancelKeyPress += (sender, e) => { module.Shutdown(); e.Cancel = true; };
            runTask.Wait();
        }
    }
}
