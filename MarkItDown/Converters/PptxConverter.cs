// Converters/PptxConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using ReverseMarkdown;
using MarkItDownSharp.Models;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using DocumentFormat.OpenXml;

namespace MarkItDownSharp.Converters
{
    public class PptxConverter : DocumentConverter
    {
        private readonly Converter _mdConverter = new Converter(new Config
        {
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
        });

        public override bool CanConvertUrl(string url) => false;

        public override bool CanConvertFile(string extension) =>
            extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase);

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var mdContent = new StringBuilder();

            try
            {
                using (var presentation = PresentationDocument.Open(localPath, false))
                {
                    var presentationPart = presentation.PresentationPart;
                    var slides = presentationPart.SlideParts.ToList();

                    for (int slideIndex = 0; slideIndex < slides.Count; slideIndex++)
                    {
                        var slide = slides[slideIndex];
                        mdContent.AppendLine($"\n\n<!-- Slide number: {slideIndex + 1} -->\n");

                        ProcessSlideShapes(slide.Slide, mdContent, options);

                        ProcessNotes(slide, mdContent);
                    }
                }
            }
            catch
            {
                return new DocumentConverterResult { TextContent = "Error processing presentation" };
            }

            return new DocumentConverterResult
            {
                Title = null,
                TextContent = mdContent.ToString().Trim()
            };
        }

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            var result = await ConvertAsync(pathOrUrl, options);
            return result != null ? new List<DocumentConverterResult> { result } : new List<DocumentConverterResult>();
        }

        private void ProcessSlideShapes(Slide slide, StringBuilder mdContent, ConversionOptions options)
        {
            var titleShape = slide.Descendants<Shape>()
                .FirstOrDefault(s => IsTitleShape(s));

            foreach (var shape in slide.Descendants<Shape>())
            {
                ProcessShape(shape, mdContent, titleShape, options);
            }

            foreach (var graphicFrame in slide.Descendants<GraphicFrame>())
            {
                ProcessGraphicFrame(graphicFrame, mdContent);
            }
        }

        private void ProcessShape(Shape shape, StringBuilder mdContent, Shape titleShape, ConversionOptions options)
        {
            // Handle group shapes recursively
            if (IsGroupShape(shape))
            {
                foreach (var childShape in shape.Descendants<Shape>())
                {
                    ProcessShape(childShape, mdContent, titleShape, options);
                }
                return;
            }

            if (IsPictureShape(shape))
            {
                ProcessImage(shape, mdContent, options);
            }
            else if (IsTableShape(shape))
            {
                ProcessTable(shape, mdContent);
            }
            else if (HasTextFrame(shape))
            {
                ProcessTextFrame(shape, mdContent, titleShape);
            }
        }

        private void ProcessTextFrame(Shape shape, StringBuilder mdContent, Shape titleShape)
        {
            var text = GetShapeText(shape).Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (shape == titleShape)
            {
                mdContent.AppendLine($"# {text}\n");
            }
            else
            {
                mdContent.AppendLine($"{text}\n");
            }
        }

        private void ProcessImage(Shape shape, StringBuilder mdContent, ConversionOptions options)
        {
            var imagePart = GetImagePart(shape);
            if (imagePart == null) return;

            var altText = GetImageAltText(shape) ?? SanitizeFilename(shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value);
            var fileName = $"{SanitizeFilename(shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value)}.jpg";

            mdContent.AppendLine($"![{altText}]({fileName})");
        }

        private void ProcessTable(Shape shape, StringBuilder mdContent)
        {
            var table = shape.Descendants<DocumentFormat.OpenXml.Drawing.Table>().FirstOrDefault();
            if (table == null) return;

            var htmlTable = new StringBuilder("<table>");
            bool firstRow = true;

            foreach (var row in table.Elements<DocumentFormat.OpenXml.Drawing.TableRow>())
            {
                htmlTable.Append("<tr>");
                foreach (var cell in row.Elements<DocumentFormat.OpenXml.Drawing.TableCell>())
                {
                    var tag = firstRow ? "th" : "td";
                    htmlTable.Append($"<{tag}>{System.Net.WebUtility.HtmlEncode(GetCellText(cell))}</{tag}>");
                }
                htmlTable.Append("</tr>");
                firstRow = false;
            }

            htmlTable.Append("</table>");
            mdContent.AppendLine(_mdConverter.Convert(htmlTable.ToString()));
        }

        private void ProcessGraphicFrame(GraphicFrame graphicFrame, StringBuilder mdContent)
        {
            var chart = graphicFrame.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Chart>().FirstOrDefault();
            if (chart != null)
            {
                mdContent.AppendLine(ProcessChart(chart));
            }
        }

        private string ProcessChart(DocumentFormat.OpenXml.Drawing.Charts.Chart chart)
        {
            var md = new StringBuilder("\n\n### Chart");
            var title = chart.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Title>().FirstOrDefault()?.InnerText;
            if (!string.IsNullOrEmpty(title))
            {
                md.Append($": {title}");
            }
            md.AppendLine("\n");

            // Simplified chart data extraction
            var categories = chart.Descendants<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData>()
                .FirstOrDefault()?.Descendants<DocumentFormat.OpenXml.Drawing.Charts.StringPoint>()
                .Select(sp => sp.InnerText);

            var series = chart.Descendants<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>()
                .Select(s => new {
                    Name = s.Descendants<DocumentFormat.OpenXml.Drawing.Charts.SeriesText>().FirstOrDefault()?.InnerText,
                    Values = s.Descendants<DocumentFormat.OpenXml.Drawing.Charts.NumericValue>()
                             .Select(nv => nv.InnerText)
                });

            if (categories != null && series.Any())
            {
                md.AppendLine("| Category | " + string.Join(" | ", series.Select(s => s.Name)) + " |");
                md.AppendLine("|" + string.Join("|", Enumerable.Repeat("---", series.Count() + 1)) + "|");

                foreach (var (category, index) in categories.Select((c, i) => (c, i)))
                {
                    var values = series.Select(s => s.Values?.ElementAtOrDefault(index));
                    md.AppendLine($"| {category} | " + string.Join(" | ", values) + " |");
                }
            }

            return md.ToString();
        }

        private void ProcessNotes(SlidePart slidePart, StringBuilder mdContent)
        {
            var notesSlide = slidePart.NotesSlidePart?.NotesSlide;
            if (notesSlide == null) return;

            var notesText = GetNotesText(notesSlide);
            if (!string.IsNullOrEmpty(notesText))
            {
                mdContent.AppendLine("\n### Notes:");
                mdContent.AppendLine(notesText);
            }
        }

        // Helper methods
        private string SanitizeFilename(string name) =>
            Regex.Replace(name ?? "", @"\W+", "");

        private bool IsTitleShape(Shape shape) =>
            shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                .GetFirstChild<PlaceholderShape>()?.Type?.Value == PlaceholderValues.Title;

        private bool IsGroupShape(Shape shape) =>
            shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.StartsWith("Group") == true;

        private bool IsPictureShape(Shape shape) =>
            shape.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();

        private bool IsTableShape(Shape shape) =>
            shape.Descendants<DocumentFormat.OpenXml.Drawing.Table>().Any();

        private bool HasTextFrame(Shape shape) =>
            shape.TextBody != null;

        private string GetShapeText(Shape shape) =>
            string.Join("\n", shape.TextBody?.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Select(t => t.Text) ?? Enumerable.Empty<string>());

        private string GetCellText(DocumentFormat.OpenXml.Drawing.TableCell cell) =>
            string.Join(" ", cell.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(t => t.Text));

        private string GetImageAltText(Shape shape) =>
            shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Description;

        private ImagePart GetImagePart(Shape shape)
        {
            var blip = shape.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
            if (blip == null) return null;

            var slidePart = shape.Ancestors<OpenXmlPartRootElement>().OfType<SlidePart>().FirstOrDefault();
            return slidePart?.GetPartById(blip.Embed?.Value) as ImagePart;
        }

        private string GetNotesText(NotesSlide notesSlide) =>
            string.Join("\n", notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Select(t => t.Text));
    }
}
