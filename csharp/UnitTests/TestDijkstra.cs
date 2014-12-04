using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;
using BridgeChat.ConversionFramework;

namespace UnitTests
{
    [TestFixture]
    public class TestDijkstra
    {
        private static readonly int[,] TestData = new int[,] {
            { 0, 3,-1,-1 },
            { 1, 0,-1, 7 },
            { 1,-1, 0,-1 },
            {-1,-1, 3, 0 },
        };

        [Test]
        public void SimpleTest()
        {
            var linqFtw = from i in Enumerable.Range(0, TestData.GetLength(0))
                          from j in Enumerable.Range(0, TestData.GetLength(1))
                          let value = TestData[i, j]
                          where value >= 0
                          select new { i, j, value };

            var dijkstra = new GenericDijkstra<int>(linqFtw.ToLookup(x => x.i, x => Tuple.Create(x.j, x.value)));
            ILookup<int, int> nextNode;
            IDictionary<int, int> cost;
            dijkstra.Run(0, out nextNode, out cost);

            Assert.AreEqual(0, cost[0]);
            Assert.AreEqual(3, cost[1]);
            Assert.AreEqual(13, cost[2]);
            Assert.AreEqual(10, cost[3]);

            Assert.IsTrue(nextNode[0].SequenceEqual(new int[] { }));
            Assert.IsTrue(nextNode[1].SequenceEqual(new int[] { 1 }));
            Assert.IsTrue(nextNode[2].SequenceEqual(new int[] { 1, 3, 2 }));
            Assert.IsTrue(nextNode[3].SequenceEqual(new int[] { 1, 3 }));
        }
    }
}

