// Models/DocumentConverterResult.cs

namespace MarkItDown.Models
{
    public class DocumentConverterResult
    {
        public DocumentConverterResult(string title = null, string textContent = "")
        {
            Title = title;
            TextContent = textContent;
        }

        public string Title { get; set; }
        public string TextContent { get; set; }
    }
}