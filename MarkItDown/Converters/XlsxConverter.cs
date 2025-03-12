// Converters/XlsxConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using MarkItDownSharp.Exceptions;
using MarkItDownSharp.Helpers;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public class XlsxConverter : DocumentConverter
    {
        private readonly CustomMarkdownConverter _markdownConverter;

        public XlsxConverter()
        {
            _markdownConverter = new CustomMarkdownConverter();
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            try
            {
                using (var workbook = new XLWorkbook(pathOrUrl))
                {
                    var sb = new StringBuilder();
                    var title = Path.GetFileNameWithoutExtension(pathOrUrl);

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        sb.AppendLine($"## {worksheet.Name}\n");

                        var table = worksheet.RangeUsed();
                        if (table == null)
                        {
                            sb.AppendLine("_No data in this sheet._\n");
                            continue;
                        }

                        var rows = table.RowsUsed().ToList();
                        if (rows.Count == 0)
                        {
                            sb.AppendLine("_No data in this sheet._\n");
                            continue;
                        }

                        // Use the first row as headers
                        var headerRow = rows.First();
                        var headers = headerRow.Cells().Select(c => c.Value.ToString().Replace("|", "\\|")).ToList();
                        sb.AppendLine("| " + string.Join(" | ", headers) + " |");
                        sb.AppendLine("|" +
                                      string.Join("|", headers.Select(h => new string('-', Math.Max(3, h.Length)))) +
                                      "|");

                        // Add data rows
                        foreach (var row in rows.Skip(1))
                        {
                            var cells = row.Cells().Select(c => c.Value.ToString().Replace("|", "\\|")).ToList();
                            sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                        }

                        sb.AppendLine("\n");
                    }

                    // Optionally, set the title based on the first worksheet name if available
                    var firstSheet = workbook.Worksheets.FirstOrDefault();
                    if (firstSheet != null && !string.IsNullOrWhiteSpace(firstSheet.Name)) title = firstSheet.Name;

                    var markdownContent = _markdownConverter.ConvertToMarkdown(sb.ToString());

                    return new DocumentConverterResult
                    {
                        Title = title,
                        TextContent = markdownContent.Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., corrupted file) as needed
                throw new ConversionException($"Failed to convert Excel file '{pathOrUrl}': {ex.Message}", ex);
            }
        }

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            var result = await ConvertAsync(pathOrUrl, options);
            return result != null ? new List<DocumentConverterResult> { result } : new List<DocumentConverterResult>();
        }
    }
}