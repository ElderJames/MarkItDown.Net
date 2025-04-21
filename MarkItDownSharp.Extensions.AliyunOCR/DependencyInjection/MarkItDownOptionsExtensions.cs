using System;
using MarkItDownSharp.DependencyInjection;
using MarkItDownSharp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MarkItDownSharp.Extensions.AliyunOCR
{
    public static class MarkItDownOptionsExtensions
    {
        /// <summary>
        /// 使用阿里云OCR服务
        /// </summary>
        public static MarkItDownOptions UseAliyunOcr(this MarkItDownOptions options, Action<AliyunOcrOptions> configure)
        {
            // 注册配置
            options.Services.PostConfigure<AliyunOcrOptions>(configure);

            // 替换默认的OCR服务实现
            options.Services.Replace(ServiceDescriptor.Singleton<IOcrService, AliyunOcrService>());

            return options;
        }

        /// <summary>
        /// 使用阿里云OCR服务
        /// </summary>
        public static MarkItDownOptions UseAliyunOcr(this MarkItDownOptions options, string accessKeyId, string accessKeySecret)
        {
            return options.UseAliyunOcr(opt =>
            {
                opt.AccessKeyId = accessKeyId;
                opt.AccessKeySecret = accessKeySecret;
            });
        }
    }
}