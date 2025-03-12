// Converters/ConfluenceConverter.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MarkItDownSharp.Exceptions;
using MarkItDownSharp.Helpers;
using MarkItDownSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarkItDownSharp.Converters
{
    public class ConfluenceConverter : DocumentConverter
    {
        private readonly HttpClient _httpClient;
        private readonly CustomMarkdownConverter _markdownConverter;

        public ConfluenceConverter()
        {
            _httpClient = new HttpClient();
            _markdownConverter = new CustomMarkdownConverter();
        }

        /// <summary>
        /// Checks if the given URL is likely a Confluence URL (page or space overview).
        /// </summary>
        public override bool CanConvertUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            url = url.ToLowerInvariant();
            // Accept URLs that include "confluence" or "atlassian.net" plus common keywords.
            return url.Contains("confluence") || url.Contains("atlassian.net") ||
                   url.Contains("viewpage.action") || url.Contains("/spaces/");
        }

        public override bool CanConvertFile(string extension)
        {
            // This converter does not handle local file formats.
            return false;
        }

        /// <summary>
        /// Main conversion entry point that returns a single DocumentConverterResult.
        /// (For a space URL, please use ConvertToListAsync.)
        /// </summary>
        /// <param name="pathOrUrl">The Confluence page or space URL.</param>
        /// <param name="options">Conversion options (can include ConfluenceBaseUrl, ConfluenceUsername, ConfluenceApiToken, ConfluenceExpand, ConfluencePageLimit, and ConfluenceMaxPages).</param>
        /// <returns>A single DocumentConverterResult if the URL represents one page.</returns>
        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!IsValidUrl(pathOrUrl))
                throw new ConversionException($"Invalid URL: {pathOrUrl}");

            options.Url = pathOrUrl; // store reference URL

            // Determine the Confluence base URL.
            string baseUrl = !string.IsNullOrEmpty(options.ConfluenceBaseUrl)
                ? options.ConfluenceBaseUrl.TrimEnd('/')
                : GetBaseUrlFromUrl(pathOrUrl);

            // Set up basic authentication if provided.
            if (!string.IsNullOrEmpty(options.ConfluenceUsername) && !string.IsNullOrEmpty(options.ConfluenceApiToken))
            {
                string username = options.ConfluenceUsername;
                string token = options.ConfluenceApiToken;
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{token}");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            // For a space (multi‐page) URL, we now require the caller to use ConvertToListAsync.
            if (IsSpaceUrl(pathOrUrl))
            {
                throw new ConversionException("For a space URL with multiple pages, please call ConvertToListAsync to retrieve a list of DocumentConverterResult items.");
            }
            else
            {
                // Otherwise, assume a single page URL.
                string pageId = ExtractPageId(pathOrUrl);
                if (string.IsNullOrEmpty(pageId))
                    throw new ConversionException("Unable to extract Confluence page id from URL.");

                // Use the configured ConfluenceExpand option (default "body.view") together with metadata.labels.
                string expandParam = (options.ConfluenceExpand ?? "body.view") + ",metadata.labels";
                string apiUrl = $"{baseUrl}/rest/api/content/{pageId}?expand={expandParam}";

                JObject content = await GetApiContentAsync(apiUrl);
                DocumentConverterResult result = await ConvertSinglePageAsync(content, baseUrl, options);
                return result;
            }
        }

        /// <summary>
        /// New method to convert a Confluence URL to a list of DocumentConverterResult items.
        /// For a space URL (normally containing many pages) a separate DocumentConverterResult is returned for each page.
        /// For a single page URL, a one‐item list is returned.
        /// </summary>
        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!IsValidUrl(pathOrUrl))
                throw new ConversionException($"Invalid URL: {pathOrUrl}");

            options.Url = pathOrUrl;
            string baseUrl = !string.IsNullOrEmpty(options.ConfluenceBaseUrl)
                ? options.ConfluenceBaseUrl.TrimEnd('/')
                : GetBaseUrlFromUrl(pathOrUrl);

            // Set up basic authentication if provided.
            if (!string.IsNullOrEmpty(options.ConfluenceUsername) && !string.IsNullOrEmpty(options.ConfluenceApiToken))
            {
                string username = options.ConfluenceUsername;
                string token = options.ConfluenceApiToken;
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{token}");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            if (IsSpaceUrl(pathOrUrl))
            {
                return await ConvertSpaceToListAsync(pathOrUrl, baseUrl, options);
            }
            else
            {
                // A single page URL: return a one‐item list.
                string pageId = ExtractPageId(pathOrUrl);
                if (string.IsNullOrEmpty(pageId))
                    throw new ConversionException("Unable to extract Confluence page id from URL.");

                string expandParam = (options.ConfluenceExpand ?? "body.view") + ",metadata.labels";
                string apiUrl = $"{baseUrl}/rest/api/content/{pageId}?expand={expandParam}";

                JObject content = await GetApiContentAsync(apiUrl);
                DocumentConverterResult result = await ConvertSinglePageAsync(content, baseUrl, options);
                return new List<DocumentConverterResult> { result };
            }
        }

        /// <summary>
        /// Private helper that converts a space URL into a list of DocumentConverterResult – one per page.
        /// </summary>
        private async Task<List<DocumentConverterResult>> ConvertSpaceToListAsync(string spaceUrl, string baseUrl, ConversionOptions options)
        {
            // Extract the space key from the URL.
            string spaceKey = ExtractSpaceKey(spaceUrl);
            if (string.IsNullOrEmpty(spaceKey))
                throw new ConversionException("Unable to extract Confluence space key from URL.");

            int start = 0;
            bool morePages = true;
            var results = new List<DocumentConverterResult>();

            while (morePages)
            {
                string apiUrl = $"{baseUrl}/rest/api/content?spaceKey={spaceKey}&expand=metadata.labels&limit={options.ConfluencePageLimit}&start={start}";
                JObject resultObj = await GetApiContentAsync(apiUrl);

                var pagesArray = resultObj["results"] as JArray;
                if (pagesArray == null || pagesArray.Count == 0)
                    break;

                foreach (var page in pagesArray)
                {
                    string id = page["id"]?.ToString() ?? "";
                    // For each page, call the detailed endpoint with our chosen expand option.
                    string expandParam = (options.ConfluenceExpand ?? "body.view") + ",metadata.labels";
                    string pageDetailUrl = $"{baseUrl}/rest/api/content/{id}?expand={expandParam}";
                    JObject detailedPage = await GetApiContentAsync(pageDetailUrl);

                    // Use the same conversion for a single page.
                    DocumentConverterResult pageResult = await ConvertSinglePageAsync(detailedPage, baseUrl, options);
                    results.Add(pageResult);

                    // If a maximum number of pages to process was set, exit early.
                    if (options.ConfluenceMaxPages > 0 && results.Count >= options.ConfluenceMaxPages)
                        return results;
                }

                int size = resultObj["size"]?.ToObject<int>() ?? 0;
                int total = resultObj["totalSize"]?.ToObject<int>() ?? 0;
                start += size;
                morePages = start < total;
            }

            return results;
        }

        /// <summary>
        /// Helper method to get JSON content from a Confluence REST API endpoint.
        /// </summary>
        private async Task<JObject> GetApiContentAsync(string apiUrl)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(apiUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new ConversionException($"Failed to retrieve Confluence content from API: {apiUrl}", ex);
            }

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<JObject>(json);
        }

        /// <summary>
        /// Converts a single page JSON object into a DocumentConverterResult.
        /// The specific property extracted from the "body" (for example "view" or "storage") is chosen based on the ConfluenceExpand option.
        /// </summary>
        private async Task<DocumentConverterResult> ConvertSinglePageAsync(JObject content, string baseUrl, ConversionOptions options)
        {
            // Determine which body expansion is being used (e.g., "body.view" or "body.storage").
            string expandOption = options.ConfluenceExpand ?? "body.view";
            string bodyKey = "view";
            if (expandOption.StartsWith("body.", StringComparison.OrdinalIgnoreCase))
            {
                bodyKey = expandOption.Substring("body.".Length);
            }

            // Extract the HTML content using the determined bodyKey.
            string htmlContent = content["body"]?[bodyKey]?["value"]?.ToString() ?? "";

            // Enhance the HTML content by fetching full plugin content (e.g., page tree)
            htmlContent = await GetFullHtmlContentAsync(baseUrl, htmlContent);

            // Convert HTML to Markdown.
            string markdown = _markdownConverter.ConvertToMarkdown(htmlContent);
            string title = content["title"]?.ToString() ?? "";
            string id = content["id"]?.ToString() ?? "";
            string webui = content["_links"]?["webui"]?.ToString() ?? "";
            string sourceLink = baseUrl + webui;

            var metaData = new Dictionary<string, object>
            {
                { "title", title },
                { "id", id },
                { "source", sourceLink }
            };

            var labels = new List<string>();
            if (content["metadata"]?["labels"]?["results"] != null)
            {
                foreach (var label in content["metadata"]["labels"]["results"])
                {
                    labels.Add(label["name"].ToString());
                }
                if (labels.Any())
                {
                    metaData.Add("labels", labels);
                }
            }

            return new DocumentConverterResult(title, markdown)
            {
                MetaData = metaData
            };
        }

        /// <summary>
        /// Helper method that enhances the provided HTML content by detecting and retrieving additional content for known Confluence plugins/macros.
        /// Currently, this method calls a processor for the page tree macro.
        /// </summary>
        private async Task<string> GetFullHtmlContentAsync(string baseUrl, string htmlContent)
        {
            // Process the page tree plugin.
            htmlContent = await ProcessPageTreeMacroAsync(baseUrl, htmlContent);
            // In the future, you can add additional plugin/macro processors here.
            return htmlContent;
        }

        /// <summary>
        /// Searches for a page tree macro placeholder in the HTML and replaces it with full HTML content
        /// retrieved by issuing a POST request to the page tree endpoint. This method first looks for a fieldset
        /// that contains the hidden input "treeRequestId" and uses all hidden inputs in that block as form parameters.
        /// </summary>
        private async Task<string> ProcessPageTreeMacroAsync(string baseUrl, string htmlContent)
        {
            // TODO: Implement the page tree macro processor.

            return htmlContent;
        }



        /// <summary>
        /// Returns true if the URL represents a Confluence space overview page.
        /// (Our assumption is that a space URL contains "/spaces/" and "/overview".)
        /// </summary>
        private bool IsSpaceUrl(string url)
        {
            url = url.ToLowerInvariant();
            return url.Contains("/spaces/") && url.Contains("/overview");
        }

        /// <summary>
        /// Attempts to extract the Confluence page ID from the URL.
        /// Looks for a pageId query parameter or the last numeric segment in the path.
        /// </summary>
        private string ExtractPageId(string url)
        {
            // Check for a pageId query parameter.
            var match = Regex.Match(url, @"[?&]pageId=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;

            // Otherwise try to extract the last numeric path segment.
            try
            {
                Uri uri = new Uri(url);
                string[] segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var segment in segments.Reverse())
                {
                    if (Regex.IsMatch(segment, @"^\d+$"))
                        return segment;
                }
            }
            catch
            {
                // Fall-through.
            }

            return null;
        }

        /// <summary>
        /// Extracts the space key from a Confluence space URL.
        /// For example, from "https://yourcompany.atlassian.net/wiki/spaces/demo/overview" returns "demo".
        /// </summary>
        private string ExtractSpaceKey(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string[] segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                // Look for the "spaces" segment and then grab the next part.
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (segments[i].Equals("spaces", StringComparison.OrdinalIgnoreCase))
                        return segments[i + 1];
                }
            }
            catch
            {
                // Fall-through.
            }
            return null;
        }

        /// <summary>
        /// Derives the base URL from the given URL.
        /// For example, from "https://yourcompany.atlassian.net/wiki/spaces/SPACE/pages/123456/Page+Title"
        /// returns "https://yourcompany.atlassian.net/wiki".
        /// </summary>
        private string GetBaseUrlFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string basePath = uri.Segments.Length > 1 ? uri.Segments[1].TrimEnd('/') : "";
                return $"{uri.Scheme}://{uri.Host}{(string.IsNullOrEmpty(basePath) ? "" : "/" + basePath)}";
            }
            catch
            {
                return url.TrimEnd('/');
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
