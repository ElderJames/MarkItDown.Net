// MarkItDown.Tests/BaseConverterTests.cs

using MarkItDownSharp.Models;

namespace MarkItDownSharp.Tests;

public abstract class BaseConverterTests
{
    protected readonly MarkItDownConverter Converter;
    protected readonly string TestDataPath;

    public BaseConverterTests()
    {
        Converter = new MarkItDownConverter();
        TestDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
    }

    protected async Task<DocumentConverterResult> ConvertAsync(string relativePathOrUrl)
    {
        var fullPathOrUrl = Path.Combine(TestDataPath, relativePathOrUrl);
        if (File.Exists(fullPathOrUrl)) return await Converter.ConvertLocalAsync(fullPathOrUrl);

        // Assume it's a URL
        return await Converter.ConvertLocalAsync(relativePathOrUrl);
    }
}