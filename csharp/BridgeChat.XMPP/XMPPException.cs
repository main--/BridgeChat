using System;
using agsXMPP.Xml.Dom;

namespace BridgeChat.XMPP
{
    public class XMPPException : Exception
    {
        public Element Element { get; private set; }

        public XMPPException(Element element)
            : base(element.ToString())
        {
            Element = element;
        }
    }
}

