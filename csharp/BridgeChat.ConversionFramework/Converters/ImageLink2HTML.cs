using System;
using System.ComponentModel.Composition;

using BridgeChat.ConversionFramework.MessageType;

namespace BridgeChat.ConversionFramework.Converters
{
    [Export(typeof(IConverter))]
    public class ImageLink2HTML : ConverterBase<ImageLink, HTML>
    {
        public ImageLink2HTML()
            : base(10)
        { }

        public override HTML Convert(ImageLink input)
        {
            throw new NotImplementedException();
        }
    }
}

