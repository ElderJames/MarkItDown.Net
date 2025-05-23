﻿// MarkItDown.Tests/Converters/HtmlConverterTests.cs

namespace MarkItDownSharp.Tests.Converters;

public class PdfConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_PDFFile_ShouldReturnExpectedMarkdown()
    {
        // Arrange
        var fileName = "Sample.pdf";
        var expectedTitle = "Lorem ipsum";
        var expectedContent = "Lorem ipsum\nLorem ipsum dolor sit amet, consectetur adipiscing\nelit.";

        // Act
        var result = await ConvertAsync(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.True(TextContains(result.TextContent, expectedContent));
    }
}