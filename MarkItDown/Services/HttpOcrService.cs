using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarkItDownSharp.Services
{
    /// <summary>
    /// 基于HTTP API的OCR服务实现
    /// </summary>
    public class HttpOcrService : IOcrService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        private bool _disposed;

        public HttpOcrService(string apiKey, string apiEndpoint)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _apiEndpoint = apiEndpoint ?? throw new ArgumentNullException(nameof(apiEndpoint));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageData), "image", "image.jpg");

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            // 这里需要根据实际使用的OCR服务来解析返回的JSON
            // 下面是一个示例实现
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                // 假设JSON响应格式为: { "text": "extracted text content" }
                return document.RootElement.GetProperty("text").GetString() ?? string.Empty;
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