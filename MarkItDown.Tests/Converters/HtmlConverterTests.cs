// MarkItDown.Tests/Converters/HtmlConverterTests.cs

namespace MarkItDownSharp.Tests.Converters;

public class HtmlConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_HtmlFile_ShouldReturnExpectedMarkdown()
    {
        // Arrange
        var fileName = "Sample.html";
        var expectedTitle = "Sample HTML Document";
        var expectedContent =
            "# Welcome to MarkItDown\n\nThis is a sample paragraph to be converted to Markdown.\n\n- First item\n- Second item";

        // Act
        var result = await ConvertAsync(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.True(TextEquals(expectedContent, result.TextContent));
    }
}