using MarkItDownSharp;
using MarkItDownSharp.Converters;
using MarkItDownSharp.DependencyInjection;
using MarkItDownSharp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加MarkItDown服务及其默认转换器
        /// </summary>
        public static IServiceCollection AddMarkItDown(this IServiceCollection services)
        {
            return AddMarkItDown(services, _ => { });
        }

        /// <summary>
        /// 添加MarkItDown服务及其默认转换器，并配置选项
        /// </summary>
        public static IServiceCollection AddMarkItDown(this IServiceCollection services, Action<MarkItDownOptions> configure)
        {
            var options = new MarkItDownOptions(services);
            configure(options);

            // 注册主服务
            services.TryAddSingleton<MarkItDownConverter>();

            // 注册默认的OCR服务
            services.TryAddSingleton<IOcrService, NoOpOcrService>();

            // 按优先级顺序注册默认转换器
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Singleton<DocumentConverter, ConfluenceConverter>(),  // 最高优先级
                ServiceDescriptor.Singleton<DocumentConverter, UrlConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, ZipConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, DocxConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, XlsxConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, PptxConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, PdfConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, PlainTextConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, HtmlConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, WavConverter>(),
                ServiceDescriptor.Singleton<DocumentConverter, Mp3Converter>(),
                ServiceDescriptor.Singleton<DocumentConverter, ImageConverter>()
            });

            // 注册自定义转换器类型
            foreach (var converterType in options.ConverterTypes)
            {
                services.TryAddEnumerable(new[]
                {
                    ServiceDescriptor.Singleton(typeof(DocumentConverter), converterType)
                });
            }

            // 注册自定义转换器实例
            foreach (var converter in options.ConverterInstances)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<DocumentConverter>(converter));
            }

            return services;
        }

        /// <summary>
        /// 添加自定义转换器
        /// </summary>
        public static IServiceCollection AddConverter<T>(this IServiceCollection services) where T : DocumentConverter
        {
            services.TryAddEnumerable(new[] { ServiceDescriptor.Singleton(typeof(DocumentConverter), typeof(T)) });
            return services;
        }

        /// <summary>
        /// 添加自定义转换器实例
        /// </summary>
        public static IServiceCollection AddConverter(this IServiceCollection services, DocumentConverter converter)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<DocumentConverter>(converter));
            return services;
        }
    }
}