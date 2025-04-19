// Converters/HtmlConverter.cs

using System;
using System.Collections.Generic;
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
            // Exclude Confluence URLs so they can be handled by ConfluenceConverter
            if (url.ToLowerInvariant().Contains("confluence") ||
                url.ToLowerInvariant().Contains("atlassian.net"))
            {
                return false;
            }
            return Regex.IsMatch(url, @"^https?://");
        }


        public override bool CanConvertFile(string? extension)
        {
            return extension != null && (
                   extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".htm", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            // Check if FileExtension is provided and can be converted
            if (options.FileExtension == null || !CanConvertFile(options.FileExtension))
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

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            var result = await ConvertAsync(pathOrUrl, options);
            return result != null ? new List<DocumentConverterResult> { result } : new List<DocumentConverterResult>();
        }
    }
}