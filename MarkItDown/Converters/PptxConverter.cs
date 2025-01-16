// Converters/PptxConverter.cs

using System;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using MarkItDown.Helpers;
using MarkItDown.Models;

namespace MarkItDown.Converters
{
    public class PptxConverter : DocumentConverter
    {
        private readonly CustomMarkdownConverter _markdownConverter;

        public PptxConverter()
        {
            _markdownConverter = new CustomMarkdownConverter();
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var sb = new StringBuilder();

            using (var ppt = PresentationDocument.Open(localPath, false))
            {
                var slides = ppt.PresentationPart.SlideParts;
                var slideNumber = 0;

                foreach (var slide in slides)
                {
                    slideNumber++;
                    sb.AppendLine($"\n\n<!-- Slide number: {slideNumber} -->\n");

                    var shapes = slide.Slide.Descendants<Shape>();
                    foreach (var shape in shapes)
                        if (shape.InnerText != null)
                        {
                            var text = shape.InnerText.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                // Determine if it's a title or normal text
                                if (shape.NonVisualShapeProperties.NonVisualDrawingProperties.Name.Value.ToLower()
                                    .Contains("title"))
                                    sb.AppendLine($"# {text}\n");
                                else
                                    sb.AppendLine($"{text}\n");
                            }
                        }

                    // Handle tables, images, charts as needed
                    // Placeholder: Implement table and image extraction
                }
            }

            return new DocumentConverterResult
            {
                Title = null,
                TextContent = sb.ToString().Trim()
            };
        }
    }
}