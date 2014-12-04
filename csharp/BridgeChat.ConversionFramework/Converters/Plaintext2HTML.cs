using System;
using System.ComponentModel.Composition;

using BridgeChat.ConversionFramework.MessageType;

namespace BridgeChat.ConversionFramework.Converters
{
    [Export(typeof(IConverter))]
    public class Plaintext2HTML : ConverterBase<Plaintext, HTML>
    {
        public Plaintext2HTML()
            : base(10)
        { }

        public override HTML Convert(Plaintext input)
        {
            return new HTML { Content = "Plain2HTML: " + input.Content };
        }
    }
}

