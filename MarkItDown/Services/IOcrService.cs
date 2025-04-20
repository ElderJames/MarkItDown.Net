using System.Threading.Tasks;

namespace MarkItDownSharp.Services
{
    public interface IOcrService
    {
        /// <summary>
        /// 从图片数据中提取文本
        /// </summary>
        /// <param name="imageData">图片二进制数据</param>
        /// <returns>提取的文本内容</returns>
        Task<string> ExtractTextAsync(byte[] imageData);
    }
}