// Converters/WikipediaConverter.cs

using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MarkItDown.Helpers;
using MarkItDown.Models;

namespace MarkItDown.Converters
{
    public class WikipediaConverter : DocumentConverter
    {
        private readonly HttpClient _httpClient;
        private readonly CustomMarkdownConverter _markdownConverter;

        public WikipediaConverter(HttpClient httpClient)
        {
            _markdownConverter = new CustomMarkdownConverter();
            _httpClient = httpClient;
        }

        public override bool CanConvertUrl(string url)
        {
            return Regex.IsMatch(url, @"^https?://[a-z]{2,3}\.wikipedia\.org/");
        }

        public override bool CanConvertFile(string extension)
        {
            // This converter handles URLs, not specific file extensions
            return false;
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            // Since URLConverter ensures only URLs are passed here, proceed directly
            var htmlContent = await Task.Run(() => File.ReadAllText(pathOrUrl));
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Remove script and style tags
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? new HtmlNodeCollection(null))
                node.Remove();

            var contentDiv = doc.DocumentNode.SelectSingleNode("//div[@id='mw-content-text']");
            var titleSpan = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'mw-page-title-main')]");
            var title = titleSpan?.InnerText.Trim() ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();

            var markdownContent = "";

            if (contentDiv != null)
            {
                var bodyMarkdown = _markdownConverter.ConvertToMarkdown(contentDiv.InnerHtml);
                markdownContent = $"# {title}\n\n{bodyMarkdown}";
            }
            else
            {
                markdownContent = _markdownConverter.ConvertToMarkdown(doc.DocumentNode.InnerHtml);
            }

            return new DocumentConverterResult
            {
                Title = title,
                TextContent = markdownContent
            };
        }
    }
}