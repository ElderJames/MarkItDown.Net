namespace MarkItDownSharp.Extensions.AliyunOCR
{
    public class AliyunOcrOptions
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string AccessKeySecret { get; set; } = string.Empty;
        public string Endpoint { get; set; } = "ocr-api.cn-hangzhou.aliyuncs.com";
    }
}