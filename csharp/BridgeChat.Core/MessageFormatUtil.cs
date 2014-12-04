using System;
using System.Collections.Generic;

using BridgeChat.Protocol;
using BridgeChat.ConversionFramework.MessageType;

namespace BridgeChat.Core
{
    public static class MessageFormatUtil
    {
        public static Type ToType(this MessageFormat format) {
            switch (format) {
            case MessageFormat.Plaintext:
                return typeof(Plaintext);
            case MessageFormat.Html:
                return typeof(HTML);
            case MessageFormat.ImageLink:
                return typeof(ImageLink);
            default:
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<object> PrepareForConversionFramework(this ChatMessage msg) {
            if (msg.PlaintextSpecified)
                yield return new Plaintext { Content = msg.Plaintext };
            if (msg.HtmlSpecified)
                yield return new HTML { Content = msg.Html };
            if (msg.ImageLinkSpecified)
                yield return new ImageLink { Content = msg.ImageLink };
        }

        public static ChatMessage PostprocessAfterConversionFramework(IEnumerable<object> output) {
            var msg = new ChatMessage();
            foreach (var item in output) {
                var plaintext = item as Plaintext;
                var html = item as HTML;
                var imageLink = item as ImageLink;

                if (plaintext != null)
                    msg.Plaintext = plaintext.Content;
                else if (html != null)
                    msg.Html = html.Content;
                else if (imageLink != null)
                    msg.ImageLink = imageLink.Content;
                else
                    throw new NotImplementedException();
            }
            return msg;
        }
    }
}

