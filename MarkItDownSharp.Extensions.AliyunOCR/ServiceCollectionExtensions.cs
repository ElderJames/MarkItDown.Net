using MarkItDownSharp.Extensions.AliyunOCR.Services;
using MarkItDownSharp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarkItDownSharp.Extensions.AliyunOCR
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAliyunOcr(this IServiceCollection services, Action<AliyunOcrOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton<IOcrService, AliyunOcrService>();
            return services;
        }
    }
}