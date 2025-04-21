using System.Threading.Tasks;

namespace MarkItDownSharp.Services
{
    /// <summary>
    /// 空的OCR服务实现，当没有配置具体OCR服务时使用
    /// </summary>
    public class NoOpOcrService : IOcrService
    {
        public Task<string> ExtractTextAsync(byte[] imageData)
        {
            // 返回空字符串，表示没有OCR功能
            return Task.FromResult(string.Empty);
        }
    }
}