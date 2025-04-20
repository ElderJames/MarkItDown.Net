using System.Text;
using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Ocr_api20210707;
using AlibabaCloud.SDK.Ocr_api20210707.Models;
using MarkItDownSharp.Services;
using Tea;

namespace MarkItDownSharp.Extensions.AliyunOCR.Services
{
    public class AliyunOcrService : IOcrService
    {
        private readonly Client _client;

        public AliyunOcrService(AliyunOcrOptions options)
        {
            var config = new Config
            {
                AccessKeyId = options.AccessKeyId,
                AccessKeySecret = options.AccessKeySecret,
                Endpoint = options.Endpoint
            };
            _client = new Client(config);
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            try
            {
                var request = new RecognizeAllTextRequest
                {
                    Body = imageData,
                    Type = "Advanced",
                    OutputTable = true,
                    OutputParagraph = true
                };

                var response = await _client.RecognizeAllTextAsync(request);
                return ConvertToMarkdown(response.Body);
            }
            catch (TeaException ex)
            {
                Console.WriteLine($"Aliyun OCR Error: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string ConvertToMarkdown(RecognizeAllTextResponse.Types.RecognizeAllTextResponseBody response)
        {
            try
            {
                var markdownBuilder = new StringBuilder();
                var data = response.Data;

                if (data?.Content == null) return string.Empty;

                // 处理段落
                if (data.Content.ParagraphInfos?.Count > 0)
                {
                    foreach (var paragraph in data.Content.ParagraphInfos)
                    {
                        markdownBuilder.AppendLine(paragraph.Text);
                        markdownBuilder.AppendLine();
                    }
                }

                // 处理表格
                if (data.Content.TableInfos?.Count > 0)
                {
                    foreach (var table in data.Content.TableInfos)
                    {
                        if (table.Cells == null || table.Cells.Count == 0) continue;

                        var rows = table.Cells
                            .GroupBy(c => c.RowIndex)
                            .OrderBy(g => g.Key);

                        var tableBuilder = new StringBuilder();
                        var firstRow = true;

                        foreach (var row in rows)
                        {
                            var sortedCells = row.OrderBy(c => c.ColumnIndex);
                            tableBuilder.AppendLine("|" + string.Join("|", sortedCells.Select(c => c.Text)) + "|");

                            if (firstRow)
                            {
                                tableBuilder.AppendLine("|" + string.Join("|", row.Select(_ => "---")) + "|");
                                firstRow = false;
                            }
                        }

                        markdownBuilder.AppendLine(tableBuilder.ToString());
                        markdownBuilder.AppendLine();
                    }
                }

                return markdownBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting to markdown: {ex.Message}");
                return string.Empty;
            }
        }
    }
}