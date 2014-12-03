using System;

namespace BridgeChat.ConversionFramework
{
    public abstract class ConverterBase<I, O> : IConverter
    {
        private readonly int Cost;

        public ConverterBase(int cost)
        {
            Cost = cost;
        }

        public abstract O Convert(I input);

        object IConverter.Convert(object input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Convert((I)input);
        }

        Type IConverter.Input { get { return typeof(I); } }
        Type IConverter.Output { get { return typeof(O); } }
        int IConverter.Cost { get { return Cost; } }
    }
}
