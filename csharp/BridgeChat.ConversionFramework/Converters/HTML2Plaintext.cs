using System;
using System.ComponentModel.Composition;

using BridgeChat.ConversionFramework.MessageType;

namespace BridgeChat.ConversionFramework.Converters
{
    [Export(typeof(IConverter))]
    public class HTML2Plaintext : ConverterBase<HTML, Plaintext>
    {
        public HTML2Plaintext()
            : base(10)
        { }

        public override Plaintext Convert(HTML input)
        {
            throw new NotImplementedException();
        }
    }
}

