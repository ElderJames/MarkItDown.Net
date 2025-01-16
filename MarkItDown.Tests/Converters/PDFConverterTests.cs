// MarkItDown.Tests/Converters/HtmlConverterTests.cs

namespace MarkItDown.Tests.Converters;

public class PDFConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_PDFFile_ShouldReturnExpectedMarkdown()
    {
        // Arrange
        var fileName = "Sample.pdf";
        var expectedTitle = "Lorem ipsum";
        var expectedContent = "Lorem ipsum\r\nLorem ipsum dolor sit amet, consectetur adipiscing\r\nelit.";

        // Act
        var result = await ConvertAsync(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Contains(expectedContent, result.TextContent);
    }
}