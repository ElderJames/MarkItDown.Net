// Converters/DocxConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ReverseMarkdown;
using MarkItDownSharp.Models;
using DocumentFormat.OpenXml;

namespace MarkItDownSharp.Converters
{
    public class DocxConverter : DocumentConverter
    {
        private static readonly Converter MarkdownConverter = new Converter(new Config
        {
            UnknownTags = Config.UnknownTagsOption.Bypass,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
        });

        public override bool CanConvertUrl(string url) => false;

        public override bool CanConvertFile(string extension) =>
            extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension))
                return null;

            var textBuilder = new StringBuilder();

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(pathOrUrl, false))
                {
                    var mainPart = doc.MainDocumentPart;
                    var body = mainPart?.Document?.Body;

                    if (body == null) return new DocumentConverterResult { TextContent = "" };

                    foreach (var element in body.ChildElements)
                    {
                        try
                        {
                            if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
                            {
                                textBuilder.AppendLine(ProcessParagraph(paragraph, mainPart));
                            }
                            else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
                            {
                                textBuilder.AppendLine(ProcessTable(table, mainPart));
                            }
                        }
                        catch
                        {
                            // Handle specific element processing errors if needed
                        }
                    }
                }
            }
            catch
            {
                return new DocumentConverterResult { TextContent = "Error processing document" };
            }

            return await Task.FromResult(new DocumentConverterResult
            {
                Title = null,
                TextContent = textBuilder.ToString().Trim()
            });
        }

        private string ProcessParagraph(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph, MainDocumentPart mainPart)
        {
            var htmlContent = new StringBuilder();
            ProcessInlineElements(paragraph.Elements<OpenXmlElement>(), htmlContent, mainPart);

            var markdown = MarkdownConverter.Convert(htmlContent.ToString().Trim());
            return AddHeadingStyle(paragraph, markdown) + AddListPrefix(paragraph, mainPart, markdown);
        }

        private void ProcessInlineElements(IEnumerable<OpenXmlElement> elements, StringBuilder htmlContent, MainDocumentPart mainPart)
        {
            foreach (var element in elements)
            {
                if (element is DocumentFormat.OpenXml.Wordprocessing.Run run)
                {
                    ProcessRun(run, htmlContent);
                }
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Hyperlink hyperlink)
                {
                    ProcessHyperlink(hyperlink, htmlContent, mainPart);
                }
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Text text)
                {
                    htmlContent.Append(text.Text);
                }
            }
        }

        private void ProcessRun(DocumentFormat.OpenXml.Wordprocessing.Run run, StringBuilder htmlContent)
        {
            var text = string.Concat(run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
            if (string.IsNullOrEmpty(text)) return;

            var props = run.RunProperties;
            var tags = new Stack<string>();

            if (props?.Bold != null) { htmlContent.Append("<strong>"); tags.Push("</strong>"); }
            if (props?.Italic != null) { htmlContent.Append("<em>"); tags.Push("</em>"); }
            if (props?.Strike != null) { htmlContent.Append("<del>"); tags.Push("</del>"); }
            if (props?.Underline != null) { htmlContent.Append("<u>"); tags.Push("</u>"); }

            htmlContent.Append(text);

            while (tags.Count > 0)
            {
                htmlContent.Append(tags.Pop());
            }

            htmlContent.Append(" ");
        }

        private void ProcessHyperlink(DocumentFormat.OpenXml.Wordprocessing.Hyperlink hyperlink, StringBuilder htmlContent, MainDocumentPart mainPart)
        {
            var url = GetHyperlinkUrl(hyperlink, mainPart);
            if (url == null) return;

            htmlContent.Append($"<a href=\"{url}\">");
            ProcessInlineElements(hyperlink.Elements<OpenXmlElement>(), htmlContent, mainPart);
            htmlContent.Append("</a> ");
        }

        private string GetHyperlinkUrl(DocumentFormat.OpenXml.Wordprocessing.Hyperlink hyperlink, MainDocumentPart mainPart)
        {
            return hyperlink.Id != null
                ? mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == hyperlink.Id)?.Uri.ToString()
                : null;
        }

        private string AddHeadingStyle(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph, string markdown)
        {
            var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var headingLevel = style?.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) == true
                ? int.TryParse(style.Substring(7), out int level) ? level : 0
                : 0;

            return headingLevel > 0 && headingLevel <= 6
                ? $"{new string('#', headingLevel)} {markdown}"
                : markdown;
        }

        private string AddListPrefix(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph, MainDocumentPart mainPart, string markdown)
        {
            var numberingProps = paragraph.ParagraphProperties?.NumberingProperties;
            if (numberingProps == null) return markdown;

            var level = numberingProps.NumberingLevelReference?.Val?.Value ?? 0;
            var indent = new string(' ', level * 2);
            return $"{indent}* {markdown}"; // Simplified bullet list handling
        }

        private string ProcessTable(DocumentFormat.OpenXml.Wordprocessing.Table table, MainDocumentPart mainPart)
        {
            var sb = new StringBuilder();
            var rows = table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().ToList();

            if (!rows.Any()) return "";

            // Process header row
            var headers = ProcessRow(rows.First(), mainPart);
            sb.AppendLine($"| {string.Join(" | ", headers)} |");
            sb.AppendLine($"|{string.Join("|", headers.Select(_ => "---"))}|");

            // Process remaining rows
            foreach (var row in rows.Skip(1))
            {
                var cells = ProcessRow(row, mainPart);
                sb.AppendLine($"| {string.Join(" | ", cells)} |");
            }

            return sb.ToString();
        }

        private List<string> ProcessRow(DocumentFormat.OpenXml.Wordprocessing.TableRow row, MainDocumentPart mainPart)
        {
            return row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
                .Select(cell =>
                {
                    var content = new StringBuilder();
                    foreach (var element in cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        content.Append(ProcessParagraph(element, mainPart));
                    }
                    return content.ToString().Replace("\n", "<br/>").Trim();
                })
                .ToList();
        }
    }
}

