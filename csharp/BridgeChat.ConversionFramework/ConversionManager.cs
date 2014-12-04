using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace BridgeChat.ConversionFramework
{
    public class ConversionManager
    {
        private class FilterConverter : IConverter
        {
            public int Cost { get { return 0; } }
            public Type Input { get { return typeof(object[]); } }
            public Type Output { get; set; }

            public object Convert(object input)
            {
                return ((object[])input).Where(x => x.GetType() == Output).Single();
            }
        }

        public static ConversionManager Instance { get; private set; }
        static ConversionManager() { Instance = new ConversionManager(); }

        private readonly IEnumerable<IConverter> Converters = new CompositionContainer(
            new AssemblyCatalog(Assembly.GetExecutingAssembly())).GetExportedValues<IConverter>();

        public IEnumerable<object> RunConversion(object[] inputs, Type[] mandatoryOutputs, Type[] optionalOutputs)
        {
            var requiredOutputs = mandatoryOutputs.ToList();
            foreach (var item in inputs) {
                var type = item.GetType();
                if (requiredOutputs.Contains(type)) {
                    requiredOutputs.Remove(type);
                    yield return item;
                } else if (optionalOutputs.Contains(type))
                    yield return item;
            }

            if (requiredOutputs.Count == 0)
                yield break; // yay, already done :D

            // oh dear, now we have to synthesize some outputs

            // simple idea: run dijkstra on what we have and use that to get where we need to go
            // this low-quality comment brought to you by: late-night coding
            var converters = Converters.Concat(inputs.Select(i => new FilterConverter { Output = i.GetType() }));
            var convertersByTypes = converters.ToDictionary(x => Tuple.Create(x.Input, x.Output));
            var dijkstra = new GenericDijkstra<Type>(converters.ToLookup(c => c.Input, c => Tuple.Create(c.Output, c.Cost)));
            ILookup<Type, Type> nextNode;
            IDictionary<Type, int> cost;
            dijkstra.Run(typeof(object[]), out nextNode, out cost);

            // TODO: optimize this by sharing conversions for each output

            foreach (var type in requiredOutputs) {
                object state = inputs;
                foreach (var step in nextNode[type])
                    state = convertersByTypes[Tuple.Create(state.GetType(), step)].Convert(state);
                yield return state;
            }
        }
    }
}

