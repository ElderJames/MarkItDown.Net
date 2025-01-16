// Converters/DocxConverter.cs

using System;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using MarkItDown.Helpers;
using MarkItDown.Models;

namespace MarkItDown.Converters
{
    public class DocxConverter : DocumentConverter
    {
        private readonly CustomMarkdownConverter _markdownConverter;

        public DocxConverter()
        {
            _markdownConverter = new CustomMarkdownConverter();
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            string htmlContent;

            using (var wordDoc = WordprocessingDocument.Open(pathOrUrl, false))
            {
                htmlContent = wordDoc.MainDocumentPart.Document.Body.InnerXml;
            }

            var markdown = _markdownConverter.ConvertToMarkdown(htmlContent);

            return new DocumentConverterResult
            {
                Title = null, // You can extract title if available
                TextContent = markdown
            };
        }
    }
}