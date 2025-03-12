// Helpers/CustomMarkdownConverter.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using ReverseMarkdown;
using ReverseMarkdown.Converters;

namespace MarkItDownSharp.Helpers
{
    public class CustomMarkdownConverter
    {
        /// <summary>
        /// Converts an HTML string to Markdown.
        /// </summary>
        /// <param name="html">The HTML string.</param>
        /// <returns>Markdown as a string.</returns>
        public string ConvertToMarkdown(string html)
        {
            // Load the HTML into an HtmlDocument
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove unwanted nodes (script, style, and macro buttons)
            RemoveNodes(doc, "//script|//style|//button[contains(@class, 'conf-macro')]");

            // Remove <img> nodes whose src attribute starts with data:image
            var imageNodes = doc.DocumentNode.SelectNodes("//img");
            if (imageNodes != null)
            {
                foreach (var img in imageNodes)
                {
                    var src = img.GetAttributeValue("src", string.Empty);
                    if (!string.IsNullOrEmpty(src) && src.StartsWith("data:image/"))
                        img.Remove();
                }
            }

            // Preprocess: Fix situations where block-level elements (like div or table)
            // are nested inside <p> tags and unwrap them.
            FixInvalidParagraphs(doc);

            // Preprocess: remove unnecessary attributes from nodes (to help the converters)
            SanitizeNodes(doc);

            // Get the cleaned-up HTML string
            var cleanedHtml = doc.DocumentNode.OuterHtml;

            // Configure ReverseMarkdown
            var config = new Config
            {
                UnknownTags = Config.UnknownTagsOption.Drop,
                GithubFlavored = true,
                RemoveComments = true,
                SmartHrefHandling = true,
                CleanupUnnecessarySpaces = true
            };

            var converter = new Converter(config);

            // Register custom converters to enhance parsing:
            // • Custom list converters so that lists inside table cells get rendered inline.
            // • An inline converter so that tags like <span> and <time> output just their text.
            // • A converter to drop unwanted macro button elements.
            converter.Register("ul", new CustomUlConverter());
            converter.Register("ol", new CustomOlConverter());
            converter.Register("button", new IgnoredTagConverter());
            converter.Register("span", new InlineTagConverter());
            converter.Register("time", new InlineTagConverter());

            // Convert the cleaned HTML to markdown
            var markdown = converter.Convert(cleanedHtml);

            return markdown;
        }

        // Remove nodes by an XPath query.
        private void RemoveNodes(HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes != null)
            {
                foreach (var node in nodes)
                    node.Remove();
            }
        }

        // Some HTML (for example from Confluence macros) can have block elements wrapped in <p> tags.
        // We “unwrap” the children so that ReverseMarkdown can process them correctly.
        private void FixInvalidParagraphs(HtmlDocument doc)
        {
            var pNodes = doc.DocumentNode.SelectNodes("//p");
            if (pNodes != null)
            {
                // Using ToList because we will modify the document.
                foreach (var p in pNodes.ToList())
                {
                    // If the paragraph contains a div or a table...
                    if (p.SelectSingleNode(".//div|.//table") != null)
                    {
                        var parent = p.ParentNode;
                        // Insert each child of the paragraph before the <p> itself…
                        foreach (var child in p.ChildNodes.ToList())
                        {
                            parent.InsertBefore(child, p);
                        }
                        // …then remove the (now empty or redundant) <p>
                        p.Remove();
                    }
                }
            }
        }

        // Remove most attributes from nodes (except for some tags like <a> and <img> that require them)
        // This way extra classes, styles, data-* attributes etc. do not interfere with conversion.
        private void SanitizeNodes(HtmlDocument doc)
        {
            // Keep attributes for these tags only.
            var whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a",
                "img"
            };

            foreach (var node in doc.DocumentNode.Descendants())
            {
                if (node.NodeType == HtmlNodeType.Element && !whitelist.Contains(node.Name))
                {
                    node.Attributes.RemoveAll();
                }
            }
        }
    }

    // Custom converter for unordered lists (<ul>)
    // If a list is inside a table cell (<td> or <th>), we convert it inline (joining items with a semicolon);
    // otherwise we output a standard markdown list.
    public class CustomUlConverter : IConverter
    {
        public string Convert(HtmlNode node)
        {
            bool isInline = node.ParentNode != null &&
                           (node.ParentNode.Name.Equals("td", StringComparison.OrdinalIgnoreCase) ||
                            node.ParentNode.Name.Equals("th", StringComparison.OrdinalIgnoreCase));

            var liNodes = node.SelectNodes("./li");
            if (liNodes == null)
                return string.Empty;

            if (isInline)
            {
                // For inline conversion, join list item texts using a semicolon.
                var items = liNodes.Select(li => li.InnerText.Trim())
                                   .Where(text => !string.IsNullOrWhiteSpace(text));
                return string.Join("; ", items);
            }
            else
            {
                // Standard markdown list conversion
                var sb = new StringBuilder();
                foreach (var li in liNodes)
                {
                    sb.AppendLine("- " + li.InnerText.Trim());
                }
                return sb.ToString();
            }
        }
    }

    // Custom converter for ordered lists (<ol>)
    // Similar logic as for <ul> converters.
    public class CustomOlConverter : IConverter
    {
        public string Convert(HtmlNode node)
        {
            bool isInline = node.ParentNode != null &&
                           (node.ParentNode.Name.Equals("td", StringComparison.OrdinalIgnoreCase) ||
                            node.ParentNode.Name.Equals("th", StringComparison.OrdinalIgnoreCase));

            var liNodes = node.SelectNodes("./li");
            if (liNodes == null)
                return string.Empty;

            if (isInline)
            {
                var items = new List<string>();
                int count = 1;
                foreach (var li in liNodes)
                {
                    var text = li.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        items.Add($"{count}. {text}");
                        count++;
                    }
                }
                return string.Join("; ", items);
            }
            else
            {
                var sb = new StringBuilder();
                int count = 1;
                foreach (var li in liNodes)
                {
                    sb.AppendLine($"{count}. " + li.InnerText.Trim());
                    count++;
                }
                return sb.ToString();
            }
        }
    }

    // Converter to ignore (drop) an element entirely.
    // For example, if a macro <button> isn’t desired in the rendered Markdown.
    public class IgnoredTagConverter : IConverter
    {
        public string Convert(HtmlNode node)
        {
            return string.Empty;
        }
    }

    // A simple inline converter that outputs only the inner text.
    // This is useful for inline elements (like <span> or <time>) whose markup we want to remove.
    public class InlineTagConverter : IConverter
    {
        public string Convert(HtmlNode node)
        {
            return node.InnerText;
        }
    }
}
