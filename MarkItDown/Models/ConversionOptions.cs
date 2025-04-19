using System.Collections.Generic;
using System.Net.Http;
using MarkItDownSharp.Converters;

namespace MarkItDownSharp.Models
{
    public class ConversionOptions
    {
        public string? FileExtension { get; set; }
        public string? Url { get; set; }
        public HttpClient? HttpClient { get; set; }
        public string? LlmClient { get; set; }
        public string? LlmModel { get; set; }
        public string? StyleMap { get; set; }

        public List<DocumentConverter> ParentConverters { get; set; } = new List<DocumentConverter>();
        public bool CleanupExtracted { get; set; } = true;

        // Existing properties to support Confluence conversion.
        public string? ConfluenceBaseUrl { get; set; }
        public string? ConfluenceUsername { get; set; }
        public string? ConfluenceApiToken { get; set; }

        // New properties for Confluence conversion customization.
        // ConfluencePageLimit specifies the number of pages retrieved per API call for space conversions.
        public int ConfluencePageLimit { get; set; } = 50;
        // ConfluenceMaxPages limits the total number of pages to process (set to 0 for unlimited).
        public int ConfluenceMaxPages { get; set; } = 0;
        // ConfluenceExpand specifies the body expansion parameter to be used (for example "body.view" or "body.storage").
        public string? ConfluenceExpand { get; set; } = "body.view";
    }
}