// MarkItDown.Tests/BaseConverterTests.cs

using MarkItDown.Models;

namespace MarkItDown.Tests;

public abstract class BaseConverterTests
{
    protected readonly MarkItDownConverter _converter;
    protected readonly string _testDataPath;

    public BaseConverterTests()
    {
        _converter = new MarkItDownConverter();
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
    }

    protected async Task<DocumentConverterResult> ConvertAsync(string relativePathOrUrl)
    {
        var fullPathOrUrl = Path.Combine(_testDataPath, relativePathOrUrl);
        if (File.Exists(fullPathOrUrl)) return await _converter.ConvertLocalAsync(fullPathOrUrl);

        // Assume it's a URL
        return await _converter.ConvertLocalAsync(relativePathOrUrl);
    }
}