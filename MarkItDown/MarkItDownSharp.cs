﻿// MarkItDown/MarkItDown.cs

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MarkItDownSharp.Converters;
using MarkItDownSharp.Exceptions;
using MarkItDownSharp.Helpers;
using MarkItDownSharp.Models;

namespace MarkItDownSharp
{
    public class MarkItDownConverter
    {
        private readonly IEnumerable<DocumentConverter> _pageConverters;

        public MarkItDownConverter(IEnumerable<DocumentConverter> converters)
        {
            _pageConverters = converters.ToList();
        }

        /// <summary>
        /// Converts a local file or URL to Markdown.
        /// </summary>
        /// <param name="pathOrUrl">The local file path or URL.</param>
        /// <param name="options">Additional conversion options.</param>
        /// <returns>A DocumentConverterResult containing the conversion output.</returns>
        public async Task<DocumentConverterResult> ConvertLocalAsync(string pathOrUrl, ConversionOptions? options = null)
        {
            options = options ?? new ConversionOptions();
            options.ParentConverters = _pageConverters.ToList();

            if (UrlHelper.IsValidUrl(pathOrUrl))
            {
                // Delegate to URLConverter
                var urlConverter = _pageConverters.OfType<UrlConverter>()
                    .FirstOrDefault(c => c.CanConvertUrl(pathOrUrl));
                if (urlConverter != null)
                {
                    var result = await urlConverter.ConvertAsync(pathOrUrl, options);
                    if (result != null)
                        return result;
                }
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                    throw new FileNotFoundException($"File not found: {pathOrUrl}");

                var extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
                options.FileExtension = extension;

                foreach (var converter in _pageConverters)
                {
                    if (converter.CanConvertFile(extension))
                    {
                        var result = await converter.ConvertAsync(pathOrUrl, options);
                        if (result != null)
                            return result;
                    }
                }
            }

            throw new UnsupportedFormatException($"Unsupported input: {pathOrUrl}");
        }

        /// <summary>
        /// Converts a local file or URL to a list of DocumentConverterResult items.
        /// This method delegates to the underlying converter's ConvertToListAsync, which may return 
        /// multiple results (for example, one for each page in a Confluence space) or a single-item list.
        /// </summary>
        /// <param name="pathOrUrl">The local file path or URL.</param>
        /// <param name="options">Additional conversion options.</param>
        /// <returns>A List of DocumentConverterResult containing the conversion output.</returns>
        public async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions? options = null)
        {
            options = options ?? new ConversionOptions();
            options.ParentConverters = _pageConverters.ToList();

            if (UrlHelper.IsValidUrl(pathOrUrl))
            {
                // Look for a converter that handles the URL.
                var converter = _pageConverters.FirstOrDefault(c => c.CanConvertUrl(pathOrUrl));
                if (converter != null)
                {
                    var results = await converter.ConvertToListAsync(pathOrUrl, options);
                    if (results != null && results.Count > 0)
                        return results;
                }
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                    throw new FileNotFoundException($"File not found: {pathOrUrl}");

                var extension = Path.GetExtension(pathOrUrl).ToLowerInvariant();
                options.FileExtension = extension;

                foreach (var converter in _pageConverters)
                {
                    if (converter.CanConvertFile(extension))
                    {
                        var results = await converter.ConvertToListAsync(pathOrUrl, options);
                        if (results != null && results.Count > 0)
                            return results;
                    }
                }
            }

            throw new UnsupportedFormatException($"Unsupported input: {pathOrUrl}");
        }
    }
}