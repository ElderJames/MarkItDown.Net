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
}