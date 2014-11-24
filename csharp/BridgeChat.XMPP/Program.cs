using System;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using agsXMPP.Factory;
using agsXMPP.protocol.iq.disco;
using agsXMPP.protocol.x.muc;
using System.CodeDom.Compiler;

namespace BridgeChat.XMPP
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var module = new XMPPModule("localhost", 3127, "secret", "test.ehvag.de", "localhost", 31337);
            var runTask = module.Run();
            Console.CancelKeyPress += (sender, e) => { module.Shutdown(); e.Cancel = true; };
            runTask.Wait();
        }
    }
}
