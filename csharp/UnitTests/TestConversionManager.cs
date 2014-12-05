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

        [Test]
        public void Html2Text()
        {
            var result = ConversionManager.Instance.RunConversion(new object[] {
                new HTML { Content = "<h1>Head<em>l</em>ine</h1><a href=\"http://ehvag.de/\">anchoring</a><br>Here you go, have an image:<img src=\"https://www.google.de/images/srpr/logo11w.png\"><pre>some code\neven <strong>more</strong> code\n even with whitespace \n</pre><p><ul><li>liul1</li><li>l<strong>iu</strong>l2</li></ul><ol><li>lio<em>l1</em></li><li>liol2</li></ol>si<strong>mp<em>l</em>et</strong>ext</p>" }
            }, new Type[] {
                typeof(Plaintext)
            }, new Type[] { });

            var contentResult = ((Plaintext)result.Single()).Content;
            Assert.AreEqual("# Head*l*ine\n\n[anchoring][0]\nHere you go, have an image:![Image][1]\n\n    some code\n    even __more__ code\n     even with whitespace \n    \n*   liul1\n*   l__iu__l2\n\n1.  lio*l1*\n2.  liol2\n\nsi__mp*l*et__ext\n\n\n[0]: http://ehvag.de/\n[1]: https://www.google.de/images/srpr/logo11w.png", contentResult);
        }
    }
}

