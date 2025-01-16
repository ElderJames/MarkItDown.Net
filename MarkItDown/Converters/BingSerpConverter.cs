// Converters/BingSerpConverter.cs

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using MarkItDown.Helpers;
using MarkItDown.Models;

namespace MarkItDown.Converters
{
    public class BingSerpConverter : DocumentConverter
    {
        private readonly CustomMarkdownConverter _markdownConverter;

        public BingSerpConverter()
        {
            _markdownConverter = new CustomMarkdownConverter();
        }

        public override bool CanConvertUrl(string url)
        {
            return Regex.IsMatch(url, @"^https?://www\.bing\.com/search\?q=", RegexOptions.IgnoreCase);
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            var extension = options.FileExtension?.ToLowerInvariant();
            if (!CanConvertFile(extension)) return null;

            if (string.IsNullOrEmpty(options.Url) || !CanConvertUrl(options.Url)) return null;

            string htmlContent;
            try
            {
                htmlContent = await Task.Run(() => File.ReadAllText(pathOrUrl));
            }
            catch (Exception ex)
            {
                // Log the error as needed
                // For example: Console.WriteLine($"Error reading file: {ex.Message}");
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Parse query parameters
            Uri uri;
            try
            {
                uri = new Uri(options.Url);
            }
            catch (UriFormatException)
            {
                // Invalid URL format
                return null;
            }

            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var query = queryParams["q"] ?? "";
            var lang = queryParams["lang"] ?? "";

            // Extract metadata: total results and search time
            var totalResults = ExtractTotalResults(doc);
            var searchTime = ExtractSearchTime(doc);

            // Remove unnecessary elements
            RemoveUnnecessaryNodes(doc);

            // Decode redirect URLs
            DecodeRedirectUrls(doc);

            // Process search results
            var results = doc.DocumentNode.SelectNodes("//li[contains(@class, 'b_algo')]");
            if (results == null || !results.Any())
                return new DocumentConverterResult
                {
                    Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim(),
                    TextContent = $"## Bing Search Results for '{query}'\n\nNo results found."
                };

            // Start building Markdown content
            var markdownBuilder = new StringBuilder();
            markdownBuilder.AppendLine($"# Bing Search Results for '{query}'\n");

            if (!string.IsNullOrEmpty(totalResults)) markdownBuilder.AppendLine($"**Total Results:** {totalResults}\n");

            if (!string.IsNullOrEmpty(searchTime)) markdownBuilder.AppendLine($"**Search Time:** {searchTime}\n");

            markdownBuilder.AppendLine("## Results:\n");

            foreach (var result in results)
            {
                // Extract title
                var titleNode = result.SelectSingleNode(".//h2/a");
                var title = titleNode?.InnerText.Trim() ?? "No Title";

                // Extract URL
                var href = titleNode?.GetAttributeValue("href", "") ?? "";

                // Extract snippet
                var snippetNode = result.SelectSingleNode(".//p");
                var snippet = snippetNode?.InnerText.Trim() ?? "No Description";

                // Convert the result node to markdown using ConvertToMarkdown
                var resultHtml = result.OuterHtml;
                var mdResult = _markdownConverter.ConvertToMarkdown(resultHtml).Trim();

                // Optionally, extract and include related searches or other sections as needed

                // Append to Markdown
                markdownBuilder.AppendLine($"### [{title}]({href})\n");
                markdownBuilder.AppendLine($"{snippet}\n");
            }

            return new DocumentConverterResult
            {
                Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim(),
                TextContent = markdownBuilder.ToString().Trim()
            };
        }

        /// <summary>
        ///     Extracts the total number of search results from the Bing SERP.
        /// </summary>
        /// <param name="doc">The HtmlDocument of the SERP.</param>
        /// <returns>Total results as a string.</returns>
        private string ExtractTotalResults(HtmlDocument doc)
        {
            // Bing typically displays the total results in a span with class 'sb_count'
            var countNode = doc.DocumentNode.SelectSingleNode("//span[@class='sb_count']");
            if (countNode != null)
            {
                // Example text: "1-10 of 100,000 results"
                var text = countNode.InnerText.Trim();
                return text;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Extracts the search time from the Bing SERP.
        /// </summary>
        /// <param name="doc">The HtmlDocument of the SERP.</param>
        /// <returns>Search time as a string.</returns>
        private string ExtractSearchTime(HtmlDocument doc)
        {
            // Bing typically does not display search time prominently.
            // If available, it might be within the 'sb_count' or another specific node.
            // Adjust the selector based on actual HTML structure.
            var searchTimeNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'sb_count')]");
            if (searchTimeNode != null)
            {
                // Implement logic if search time is present in the node
                // For example, extract using regex if search time is embedded in text
                var text = searchTimeNode.InnerText.Trim();
                // Example pattern: "About 1,000,000 results (0.32 seconds)"
                var match = Regex.Match(text, @"\((.*?)\)");
                if (match.Success && match.Groups.Count > 1) return match.Groups[1].Value;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Removes unnecessary nodes from the Bing SERP HTML to clean up the content.
        /// </summary>
        /// <param name="doc">The HtmlDocument to clean.</param>
        private void RemoveUnnecessaryNodes(HtmlDocument doc)
        {
            // Remove elements that are not needed in the Markdown output
            var nodesToRemove = doc.DocumentNode.SelectNodes("//*[" +
                                                             "contains(@class, 'tptt') or " +
                                                             "contains(@class, 'algoSlug_icon') or " +
                                                             "contains(@class, 'b_ad') or " + // Example: Ads
                                                             "contains(@class, 'b_caption') or " +
                                                             "contains(@class, 'b_tween') or " +
                                                             "contains(@class, 'b_pag') or " + // Pagination
                                                             "contains(@class, 'b_context') " + // Contextual ads or related info
                                                             "]");

            if (nodesToRemove != null)
                foreach (var node in nodesToRemove)
                    node.Remove();
        }

        /// <summary>
        ///     Decodes redirect URLs in the SERP to get the actual destination URLs.
        /// </summary>
        /// <param name="doc">The HtmlDocument of the SERP.</param>
        private void DecodeRedirectUrls(HtmlDocument doc)
        {
            var anchorNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (anchorNodes == null)
                return;

            foreach (var aNode in anchorNodes)
            {
                var href = aNode.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href))
                    continue;

                try
                {
                    var uri = new Uri(href);
                    var queryParams = HttpUtility.ParseQueryString(uri.Query);
                    var encodedUrl = queryParams["u"];

                    if (!string.IsNullOrEmpty(encodedUrl))
                    {
                        // Decode using Base64Url (replace '-' with '+' and '_' with '/')
                        var base64 = encodedUrl.Replace('-', '+').Replace('_', '/');

                        // Adjust padding if necessary
                        switch (base64.Length % 4)
                        {
                            case 2: base64 += "=="; break;
                            case 3: base64 += "="; break;
                        }

                        var data = Convert.FromBase64String(base64);
                        var decodedUrl = Encoding.UTF8.GetString(data);

                        aNode.SetAttributeValue("href", decodedUrl);
                    }
                }
                catch
                {
                    // If any error occurs during decoding, retain the original href
                }
            }
        }
    }
}