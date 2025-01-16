// Models/ConversionOptions.cs

using System.Collections.Generic;
using System.Net.Http;
using MarkItDown.Converters;

namespace MarkItDown.Models
{
    public class ConversionOptions
    {
        public string FileExtension { get; set; }
        public string Url { get; set; }
        public HttpClient HttpClient { get; set; }
        public string LlmClient { get; set; }
        public string LlmModel { get; set; }
        public string StyleMap { get; set; }

        public List<DocumentConverter> ParentConverters { get; set; } = new List<DocumentConverter>();
        public bool CleanupExtracted { get; set; } = true;
    }
}