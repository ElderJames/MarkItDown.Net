using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarkItDownSharp.Services
{
    public class AliyunOcrService : IOcrService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKeyId;
        private readonly string _accessKeySecret;
        private readonly string _endpoint;
        private bool _disposed;

        public AliyunOcrService(string accessKeyId, string accessKeySecret, string endpoint = "ocr-api.cn-hangzhou.aliyuncs.com")
        {
            _accessKeyId = accessKeyId ?? throw new ArgumentNullException(nameof(accessKeyId));
            _accessKeySecret = accessKeySecret ?? throw new ArgumentNullException(nameof(accessKeySecret));
            _endpoint = endpoint;
            _httpClient = new HttpClient();
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri($"https://{_endpoint}");
            // 配置认证头，实际项目中需要按照阿里云SDK的方式计算签名
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessKeyId}:{_accessKeySecret}");
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            try
            {
                var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(imageData), "body", "image.jpg");

                // 添加必要的参数
                content.Add(new StringContent("Advanced"), "Type");
                content.Add(new StringContent("true"), "OutputTable"); // 输出表格信息
                content.Add(new StringContent("true"), "OutputParagraph"); // 输出段落信息

                var response = await _httpClient.PostAsync("/", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return ConvertToMarkdown(jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aliyun OCR Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string ConvertToMarkdown(string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;
                var data = root.GetProperty("Data");
                var content = data.GetProperty("Content");

                var markdownBuilder = new StringBuilder();

                // 处理段落
                if (content.TryGetProperty("ParagraphInfos", out var paragraphs))
                {
                    foreach (var paragraph in paragraphs.EnumerateArray())
                    {
                        var text = paragraph.GetProperty("Text").GetString();
                        markdownBuilder.AppendLine(text);
                        markdownBuilder.AppendLine();
                    }
                }

                // 处理表格
                if (content.TryGetProperty("TableInfos", out var tables))
                {
                    foreach (var table in tables.EnumerateArray())
                    {
                        var cells = table.GetProperty("Cells").EnumerateArray().ToList();
                        var rows = cells.GroupBy(c => c.GetProperty("RowIndex").GetInt32()).OrderBy(g => g.Key);

                        // 构建表格
                        var tableBuilder = new StringBuilder();
                        var firstRow = true;

                        foreach (var row in rows)
                        {
                            var sortedCells = row.OrderBy(c => c.GetProperty("ColumnIndex").GetInt32());
                            tableBuilder.AppendLine("|" + string.Join("|", sortedCells.Select(c => c.GetProperty("Text").GetString())) + "|");

                            // 添加表格分隔行
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
                Console.WriteLine($"Error parsing OCR response: {ex.Message}");
                return string.Empty;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }
}