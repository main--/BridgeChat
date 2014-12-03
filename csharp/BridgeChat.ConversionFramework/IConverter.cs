using System;

namespace BridgeChat.ConversionFramework
{
    public interface IConverter
    {
        int Cost { get; }
        Type Input { get; }
        Type Output { get; }
        object Convert(object input);
    }
}
