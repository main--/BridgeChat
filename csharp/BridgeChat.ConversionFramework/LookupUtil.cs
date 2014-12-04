using System;
using System.Linq;
using System.Collections.Generic;

namespace BridgeChat.ConversionFramework
{
    public static class LookupUtil
    {
        private class SimpleGrouping<K, V> : IGrouping<K, V>
        {
            public K Key { get; set; }
            public IEnumerable<V> Values { get; set; }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return Values.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return Values.GetEnumerator();
            }
        }

        private class DictionaryLookup<K, V> : ILookup<K, V> {
            private readonly IDictionary<K, IEnumerable<V>> Underlying;

            public DictionaryLookup(IDictionary<K, IEnumerable<V>> underlying)
            {
                Underlying = underlying;
            }

            bool ILookup<K, V>.Contains(K key)
            {
                return Underlying.ContainsKey(key);
            }

            int ILookup<K, V>.Count { get { return Underlying.Count; } }
            IEnumerable<V> ILookup<K, V>.this[K index] { get { return Underlying[index]; } }

            public IEnumerator<IGrouping<K, V>> GetEnumerator()
            {
                foreach (var pair in Underlying)
                    yield return new SimpleGrouping<K, V> { Key = pair.Key, Values = pair.Value };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static ILookup<K, V> ToLookup<K, V>(this IDictionary<K, IEnumerable<V>> input) {
            return new DictionaryLookup<K, V>(input);
        }
    }
}

