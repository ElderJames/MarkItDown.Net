// MarkItDown.Tests/Converters/HtmlConverterTests.cs

namespace MarkItDown.Tests.Converters;

public class HtmlConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_HtmlFile_ShouldReturnExpectedMarkdown()
    {
        // Arrange
        var fileName = "Sample.html";
        var expectedTitle = "Sample HTML Document";
        var expectedContent =
            "# Welcome to MarkItDown\r\n\r\nThis is a sample paragraph to be converted to Markdown.\r\n\r\n- First item\r\n- Second item";

        // Act
        var result = await ConvertAsync(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedContent, result.TextContent);
    }
}