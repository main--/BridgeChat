using System;
using System.Linq;
using System.Collections.Generic;

namespace BridgeChat.ConversionFramework
{
    public class GenericDijkstra<TNode>
    {
        private readonly TNode[] Nodes;
        private readonly int[,] Adjacency;

        public GenericDijkstra(ILookup<TNode, Tuple<TNode, int>> graph)
        {
            Nodes = graph.Select(x => x.Key).ToArray();
            Adjacency = new int[Nodes.Length, Nodes.Length];
            for (int i = 0; i < Nodes.Length; i++)
                for (int j = 0; j < Nodes.Length; j++)
                    Adjacency[i, j] = -1;

            foreach (var pair in graph) {
                var row = Array.IndexOf(Nodes, pair.Key);
                foreach (var item in pair)
                    Adjacency[row, Array.IndexOf(Nodes, item.Item1)] = item.Item2;
            }
        }

        private static IEnumerable<int> BuildReversePath(int[] prev, int source, int target)
        {
            for (int current = target; current != source; current = prev[current])
                yield return current;
        }

        public void Run(TNode start, out ILookup<TNode, TNode> nextNode, out IDictionary<TNode, int> cost)
        {
            // begin ye olde dijkstra straight from wikipedia
            var todoVertices = Enumerable.Range(0, Nodes.Length).ToList();
            var distance = Enumerable.Repeat(Int32.MaxValue, Nodes.Length).ToArray();
            var previous = Enumerable.Repeat(-1, Nodes.Length).ToArray();
            var startIndex = Array.IndexOf(Nodes, start);

            distance[startIndex] = 0;

            while (todoVertices.Count > 0) {
                var u = todoVertices.OrderBy(idx => distance[idx]).First();

                todoVertices.Remove(u);

                for (int v = 0; v < Nodes.Length; v++) {
                    if (Adjacency[u, v] >= 0) {
                        int alt = distance[u] + Adjacency[u, v];
                        if (alt < distance[v]) {
                            distance[v] = alt;
                            previous[v] = u;
                        }
                    }
                }
            }
            // end ye olde dijkstra

            // now let's reverse the paths in previous
            // so they get a nice interface
            nextNode = Enumerable.Range(0, Nodes.Length).ToDictionary(x => Nodes[x],
                target => BuildReversePath(previous, startIndex, target).Select(x => Nodes[x]).Reverse()).ToLookup();
            cost = Enumerable.Range(0, Nodes.Length).ToDictionary(x => Nodes[x], x => distance[x]);
        }
    }
}

