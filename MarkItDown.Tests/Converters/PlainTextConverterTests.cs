// MarkItDown.Tests/Converters/PlainTextConverterTests.cs

namespace MarkItDownSharp.Tests.Converters;

public class PlainTextConverterTests : BaseConverterTests
{
    [Fact]
    public async Task Convert_PlainTextFile_ShouldReturnCorrectContent()
    {
        // Arrange
        var fileName = "Sample.txt";

        // Act
        var result = await ConvertAsync(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Title); // As per PlainTextConverter implementation
        var expectedContent = await File.ReadAllTextAsync(Path.Combine(TestDataPath, fileName));
        Assert.True(TextEquals(expectedContent, result.TextContent));
    }
}