using System;
using System.Collections.Generic;
using MarkItDownSharp.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace MarkItDownSharp.DependencyInjection
{
    public class MarkItDownOptions
    {
        public IServiceCollection Services { get; }

        public MarkItDownOptions(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// 自定义转换器类型列表
        /// </summary>
        internal List<Type> ConverterTypes { get; } = new();

        /// <summary>
        /// 自定义转换器实例列表
        /// </summary>
        internal List<DocumentConverter> ConverterInstances { get; } = new();

        /// <summary>
        /// 添加自定义转换器类型
        /// </summary>
        public void AddConverter<T>() where T : DocumentConverter
        {
            ConverterTypes.Add(typeof(T));
        }

        /// <summary>
        /// 添加自定义转换器实例
        /// </summary>
        public void AddConverter(DocumentConverter converter)
        {
            ConverterInstances.Add(converter);
        }
    }
}