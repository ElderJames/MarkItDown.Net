// Converters/URLConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MarkItDownSharp.Exceptions;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public class UrlConverter : DocumentConverter
    {
        private readonly HttpClient _httpClient;
        private readonly List<DocumentConverter> _specificUrlConverters;

        public UrlConverter()
        {
            _httpClient = new HttpClient();

            // Initialize specific URL converters
            _specificUrlConverters = new List<DocumentConverter>
            {
                new ConfluenceConverter(),
                new WikipediaConverter(),
                new YouTubeConverter(),
                new BingSerpConverter(),
                new HtmlConverter()
                // Add other specific URL converters here
            };
        }

        public override bool CanConvertUrl(string url)
        {
            foreach (var converter in _specificUrlConverters)
                if (converter.CanConvertUrl(url))
                    return true;
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            // URLConverter doesn't handle file extensions directly.
            return false;
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!IsValidUrl(pathOrUrl))
                return null;

            var url = pathOrUrl;
            options.Url = url;

            // Determine which specific converter to use based on the URL.
            var selectedConverter = GetSpecificConverter(url);
            if (selectedConverter == null)
                // Fallback to a default converter, for example, HtmlConverter.
                selectedConverter = new HtmlConverter();

            // If the selected converter is ConfluenceConverter, call its ConvertAsync directly.
            if (selectedConverter is ConfluenceConverter)
            {
                return await selectedConverter.ConvertAsync(url, options);
            }

            // Otherwise, download content into a temporary file.
            var extension = GetExtensionFromUrl(url);
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);

            try
            {
                var contentBytes = await DownloadContentAsync(url);

                if (IsTextFile(extension))
                {
                    // For text files, decode the bytes to string and write as text.
                    var content = Encoding.UTF8.GetString(contentBytes);
                    await Task.Run(() => File.WriteAllText(tempFilePath, content));
                }
                else
                {
                    // For binary files, write bytes directly.
                    await Task.Run(() => File.WriteAllBytes(tempFilePath, contentBytes));
                }

                // Update the options with the determined file extension.
                options.FileExtension = extension;

                // Delegate conversion to the selected specific converter.
                var result = await selectedConverter.ConvertAsync(tempFilePath, options);
                return result;
            }
            catch (Exception ex)
            {
                throw new ConversionException($"Failed to convert URL: {url}", ex);
            }
            finally
            {
                // Clean up the temporary file.
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { /* Optionally log deletion errors */ }
                }
            }
        }

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!IsValidUrl(pathOrUrl))
                return null;

            var url = pathOrUrl;
            options.Url = url;

            // Determine which specific converter to use based on the URL.
            var selectedConverter = GetSpecificConverter(url);
            if (selectedConverter == null)
                selectedConverter = new HtmlConverter();

            // If the selected converter is ConfluenceConverter (or any converter that properly supports multiple results),
            // delegate directly to its ConvertToListAsync.
            if (selectedConverter is ConfluenceConverter)
            {
                return await selectedConverter.ConvertToListAsync(url, options);
            }

            // Otherwise, proceed to download the URL content to a temp file.
            var extension = GetExtensionFromUrl(url);
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);

            try
            {
                var contentBytes = await DownloadContentAsync(url);

                if (IsTextFile(extension))
                {
                    var content = Encoding.UTF8.GetString(contentBytes);
                    await Task.Run(() => File.WriteAllText(tempFilePath, content));
                }
                else
                {
                    await Task.Run(() => File.WriteAllBytes(tempFilePath, contentBytes));
                }

                options.FileExtension = extension;

                // Delegate to the specific converter’s ConvertToListAsync.
                var results = await selectedConverter.ConvertToListAsync(tempFilePath, options);
                return results;
            }
            catch (Exception ex)
            {
                throw new ConversionException($"Failed to convert URL to list: {url}", ex);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { /* Optionally log deletion errors */ }
                }
            }
        }

        private bool IsTextFile(string extension)
        {
            var textExtensions = new HashSet<string> { ".html", ".htm", ".txt", ".docx", ".pptx", ".xlsx", ".csv", ".md" };
            return textExtensions.Contains(extension);
        }

        private bool IsValidUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private DocumentConverter GetSpecificConverter(string url)
        {
            foreach (var converter in _specificUrlConverters)
            {
                if (converter.CanConvertUrl(url))
                    return converter;
            }
            return null;
        }

        private async Task<byte[]> DownloadContentAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        private string GetExtensionFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (!string.IsNullOrEmpty(extension))
                    return extension;
                if (url.EndsWith("/"))
                    return ".html";
                return ".html"; // Default to .html if no extension is found.
            }
            catch
            {
                return ".html"; // Fallback to .html.
            }
        }
    }
}
