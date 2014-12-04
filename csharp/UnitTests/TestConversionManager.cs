using NUnit.Framework;
using System;
using System.Linq;
using BridgeChat.ConversionFramework;
using BridgeChat.ConversionFramework.MessageType;

namespace UnitTests
{
    [TestFixture]
    public class TestConversionManager
    {
        [Test]
        public void Plain2Image()
        {
            var result = ConversionManager.Instance.RunConversion(new object[] {
                new Plaintext { Content = "simple plaintext" }
            }, new Type[] {
                typeof(ImageLink)
            }, new Type[] {
                typeof(Plaintext)
            });

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.GetType() == typeof(ImageLink))); // they do the conversion
            Assert.AreEqual(1, result.Count(x => x.GetType() == typeof(Plaintext))); // but also pass the optional
        }
    }
}

