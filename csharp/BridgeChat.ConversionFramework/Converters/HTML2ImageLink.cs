using System;
using System.ComponentModel.Composition;

using BridgeChat.ConversionFramework.MessageType;

namespace BridgeChat.ConversionFramework.Converters
{
    [Export(typeof(IConverter))]
    public class HTML2ImageLink : ConverterBase<HTML, ImageLink>
    {
        public HTML2ImageLink()
            : base(10)
        { }

        public override ImageLink Convert(HTML input)
        {
            return new ImageLink { Content = "HTML2ImageLink: " + input.Content };
        }
    }
}

