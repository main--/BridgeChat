using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using BridgeChat.ConversionFramework.MessageType;

using HtmlAgilityPack;

namespace BridgeChat.ConversionFramework.Converters
{
    [Export(typeof(IConverter))]
    public class HTML2Plaintext : ConverterBase<HTML, Plaintext>
    {
        public HTML2Plaintext()
            : base(10)
        { }

        public override Plaintext Convert(HTML input)
        {
            var linkList = new List<string>();
            var builder = new StringBuilder();
            var document = new HtmlDocument();
            document.LoadHtml(input.Content);
            RunConversion(document.DocumentNode, builder, linkList);

            // and postprocess: coerce multiple blank lines
            var result = builder.ToString();
            builder.Clear();
            bool justHadWhitespace = false;
            foreach (var part in result.Split('\n'))
                if (!(justHadWhitespace & (justHadWhitespace = String.IsNullOrWhiteSpace(part))))
                    builder.AppendLine(part);

            if (linkList.Count > 0) {
                builder.AppendLine();
                for (int i = 0; i < linkList.Count; i++)
                    builder.AppendFormat("[{0}]: {1}\n", i, linkList[i]);
            }

            return new Plaintext { Content = builder.ToString().Trim() };
        }

        private static void ConvertChildren(HtmlNode node, StringBuilder builder,
            ICollection<string> linkList, string linePrefix = "")
        {
            foreach (var child in node.ChildNodes)
                RunConversion(child, builder, linkList, linePrefix);
        }

        private static void HeadlineHelper(HtmlNode node, StringBuilder output,
            ICollection<string> linkList, int level)
        {
            for (int i = 0; i < level; i++)
                output.Append('#');
            output.Append(' ');
            ConvertChildren(node, output, linkList);
            output.AppendLine();
            output.AppendLine();
        }

        private static void RunConversion(HtmlNode node, StringBuilder output,
            ICollection<string> linkList, string linePrefix = "")
        {
            switch (node.NodeType) {
            case HtmlNodeType.Comment:
                break; // ignore comments
            case HtmlNodeType.Document:
            case HtmlNodeType.Element:
                switch (node.Name) {
                case "h1":
                    HeadlineHelper(node, output, linkList, 1);
                    break;
                case "h2":
                    HeadlineHelper(node, output, linkList, 2);
                    break;
                case "h3":
                    HeadlineHelper(node, output, linkList, 3);
                    break;
                case "h4":
                    HeadlineHelper(node, output, linkList, 4);
                    break;
                case "h5":
                    HeadlineHelper(node, output, linkList, 5);
                    break;
                case "h6":
                    HeadlineHelper(node, output, linkList, 6);
                    break;
                case "b":
                case "strong":
                    output.Append("__");
                    ConvertChildren(node, output, linkList);
                    output.Append("__");
                    break;
                case "i":
                case "em":
                    output.Append("*");
                    ConvertChildren(node, output, linkList);
                    output.Append("*");
                    break;
                case "ul":
                    output.Append("\n\n");
                    foreach (var li in node.ChildNodes) {
                        if ((li.NodeType == HtmlNodeType.Element)
                            && (li.Name == "li")) {
                            output.Append("*   ");
                            ConvertChildren(li, output, linkList);
                            output.AppendLine();
                        }
                    }
                    break;
                case "ol":
                    output.AppendLine();
                    int olIndex = 1;
                    foreach (var li in node.ChildNodes) {
                        if ((li.NodeType == HtmlNodeType.Element)
                            && (li.Name == "li")) {
                            int oldLength = output.Length;
                            output.Append(olIndex++).Append(".   ");
                            output.Length = oldLength + 4;
                            ConvertChildren(li, output, linkList);
                            output.AppendLine();
                        }
                    }
                    output.AppendLine();
                    break;
                case "pre":
                    output.AppendLine();
                    output.AppendLine();
                    ConvertChildren(node, output, linkList, "    ");
                    output.AppendLine();
                    break;
                case "p":
                    ConvertChildren(node, output, linkList);
                    output.AppendLine();
                break;
                case "hr":
                    output.AppendLine();
                    output.AppendLine("----------");
                    output.AppendLine();
                    break;
                case "a":
                    var href = node.Attributes.AttributesWithName("href").SingleOrDefault();
                    if (href != null) {
                        output.Append('[');
                        ConvertChildren(node, output, linkList);
                        output.Append("][").Append(linkList.Count).Append(']');
                        linkList.Add(href.Value);
                    } else // don't care
                        ConvertChildren(node, output, linkList);
                    break;
                case "img":
                    var imgSrc = node.Attributes.AttributesWithName("src").Single();
                    var alt = node.Attributes.AttributesWithName("alt").SingleOrDefault();
                    var altText = (alt != null) ? alt.Value : "Image";
                    output.AppendFormat("![{0}][{1}]", altText, linkList.Count);
                    linkList.Add(imgSrc.Value);
                    break;
                case "br":
                    output.AppendLine();
                    break;
                default:
                    // strip unknown tags and process what's inside of them
                    ConvertChildren(node, output, linkList);
                    break;
                    }
                break;
            case HtmlNodeType.Text:
                // just plain text
                var inner = ((HtmlTextNode)node).Text;
                if (!HtmlNode.IsOverlappedClosingElement(inner)
                    && (inner.Trim().Length > 0)) {
                    var lines = HtmlEntity.DeEntitize(inner).Split('\n');
                    if (output[output.Length - 1] == '\n')
                        output.Append(linePrefix);
                    foreach (var line in lines)
                        output.AppendLine(line).Append(linePrefix);
                    output.Length -= linePrefix.Length + 1; // remove trailing newline and prefix
                }
                break;
            default:
                throw new NotImplementedException();
            }
        }
    }
}

