// MarkItDown.Tests/BaseConverterTests.cs

using MarkItDownSharp.Converters;
using MarkItDownSharp.DependencyInjection;
using MarkItDownSharp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MarkItDownSharp.Tests;

public abstract class BaseConverterTests
{
    protected readonly MarkItDownConverter Converter;
    protected readonly string TestDataPath;
    protected readonly IServiceProvider ServiceProvider;

    public BaseConverterTests()
    {
        ServiceProvider = ConfigureServices();
        Converter = ServiceProvider.GetRequiredService<MarkItDownConverter>();
        TestDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
    }

    protected virtual IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // 添加MarkItDown服务及其默认转换器
        services.AddMarkItDown(options =>
        {
            ConfigureMarkItDown(options);
        });

        return services.BuildServiceProvider();
    }

    protected virtual void ConfigureMarkItDown(MarkItDownOptions options)
    {
        
    }

    protected async Task<DocumentConverterResult> ConvertAsync(string relativePathOrUrl)
    {
        var fullPathOrUrl = Path.Combine(TestDataPath, relativePathOrUrl);
        if (File.Exists(fullPathOrUrl)) return await Converter.ConvertLocalAsync(fullPathOrUrl);

        // Assume it's a URL
        return await Converter.ConvertLocalAsync(relativePathOrUrl);
    }

    /// <summary>
    /// 规范化文本中的换行符，使其在不同平台上保持一致
    /// </summary>
    protected string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // 先将所有换行符转换为\n，再统一转换为当前平台的换行符
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    /// <summary>
    /// 比较两个文本，忽略换行符差异
    /// </summary>
    protected bool TextEquals(string expected, string actual)
    {
        if (expected == null && actual == null) return true;
        if (expected == null || actual == null) return false;

        var normalizedExpected = NormalizeLineEndings(expected);
        var normalizedActual = NormalizeLineEndings(actual);

        return normalizedExpected == normalizedActual;
    }

    /// <summary>
    /// 检查文本是否包含指定的内容，忽略换行符差异
    /// </summary>
    /// <param name="text">要搜索的文本</param>
    /// <param name="searchFor">要查找的内容</param>
    /// <param name="ignoreCase">是否忽略大小写，默认为true</param>
    /// <returns>如果找到内容则返回true，否则返回false</returns>
    protected bool TextContains(string text, string searchFor, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchFor)) return false;

        var normalizedText = NormalizeLineEndings(text);
        var normalizedSearchFor = NormalizeLineEndings(searchFor);

        return normalizedText.Contains(normalizedSearchFor, 
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}