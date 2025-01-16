// Converters/URLConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MarkItDown.Exceptions;
using MarkItDown.Models;

namespace MarkItDown.Converters
{
    public class URLConverter : DocumentConverter
    {
        private readonly HttpClient _httpClient;
        private readonly List<DocumentConverter> _specificUrlConverters;

        public URLConverter(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();

            // Initialize specific URL converters
            _specificUrlConverters = new List<DocumentConverter>
            {
                new WikipediaConverter(_httpClient),
                new YouTubeConverter(_httpClient),
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
            // URLConverter doesn't handle file extensions directly
            return false;
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!IsValidUrl(pathOrUrl))
                return null;

            var url = pathOrUrl;
            options.Url = url;

            // Determine which specific converter to use based on the URL
            var selectedConverter = GetSpecificConverter(url);

            if (selectedConverter == null)
                // Fallback to a default converter, e.g., HtmlConverter
                selectedConverter = new HtmlConverter();

            // Determine the file extension
            var extension = GetExtensionFromUrl(url);
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);

            try
            {
                var contentBytes = await DownloadContentAsync(url);

                if (IsTextFile(extension))
                {
                    // For text files, convert bytes to string and write as text
                    var content = Encoding.UTF8.GetString(contentBytes);
                    await Task.Run(() => File.WriteAllText(tempFilePath, content));
                }
                else
                {
                    // For binary files, write bytes directly
                    await Task.Run(() => File.WriteAllBytes(tempFilePath, contentBytes));
                }

                // Update the options with the temp file path and extension
                options.FileExtension = extension;

                // Delegate conversion to the selected specific converter
                var result = await selectedConverter.ConvertAsync(tempFilePath, options);
                return result;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                throw new ConversionException($"Failed to convert URL: {url}", ex);
            }
            finally
            {
                // Ensure the temporary file is deleted
                if (File.Exists(tempFilePath))
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Log or handle the exception if needed
                    }
            }
        }

        private bool IsTextFile(string extension)
        {
            var textExtensions = new HashSet<string>
                { ".html", ".htm", ".txt", ".docx", ".pptx", ".xlsx", ".csv", ".md" };
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
                if (converter.CanConvertUrl(url))
                    return converter;
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

                return ".html"; // Default to .html if no extension is found
            }
            catch
            {
                return ".html"; // Fallback to .html in case of an invalid URL
            }
        }
    }
}