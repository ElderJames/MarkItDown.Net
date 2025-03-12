// MarkItDown.Tests/Converters/URLConverterTests.cs

namespace MarkItDownSharp.Tests.Converters;

public class URLConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_URLFile_ShouldReturnExpectedMarkdown()
    {
        // Arrange
        var url = "https://example.com";
        var expectedTitle = "Example Domain";
        var expectedContent =
            "This domain is for use in illustrative examples in documents.";

        // Act
        var result = await ConvertAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Contains(expectedContent, result.TextContent);
    }
}