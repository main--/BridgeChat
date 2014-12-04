using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace BridgeChat.ConversionFramework
{
    public class ConversionManager
    {
        public static ConversionManager Instance { get; private set; }
        static ConversionManager() { Instance = new ConversionManager(); }

        private readonly IDictionary<Type, IEnumerable<IConverter>> ConversionGraph = new Dictionary<Type, IEnumerable<IConverter>>();

        public ConversionManager()
        {
            var container = new CompositionContainer(new AssemblyCatalog(GetType().Assembly));
            var converters = container.GetExportedValues<IConverter>();
            var types = converters.Select(c => c.Input).Concat(converters.Select(c => c.Output)).Distinct();

            foreach (var type in types)
                ConversionGraph.Add(type, converters.Where(converter => converter.Input == type).ToArray());
        }

        public object[] RunConversion(object[] inputs, Type[] mandatoryOutputs, Type[] optionalOutputs)
        {
            var maybeAwesome = inputs.Where(o => mandatoryOutputs.Contains(o.GetType())).ToArray();
            if (maybeAwesome.Length != 0)
                return maybeAwesome;

            // TODO @sohalt
            throw new NotImplementedException("have fun");
        }
    }
}

