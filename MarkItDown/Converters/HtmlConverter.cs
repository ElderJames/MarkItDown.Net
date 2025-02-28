// Converters/HtmlConverter.cs

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MarkItDownSharp.Helpers;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public class HtmlConverter : DocumentConverter
    {
        private readonly CustomMarkdownConverter _markdownConverter;

        public HtmlConverter()
        {
            _markdownConverter = new CustomMarkdownConverter();
        }

        public override bool CanConvertUrl(string url)
        {
            // HtmlConverter can handle any HTML URL not handled by specific converters
            return Regex.IsMatch(url, @"^https?://");
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension))
                return null;

            var htmlContent = await Task.Run(() => File.ReadAllText(pathOrUrl));
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
            var markdown = _markdownConverter.ConvertToMarkdown(body.InnerHtml);

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();

            return new DocumentConverterResult
            {
                Title = title,
                TextContent = markdown
            };
        }
    }
}