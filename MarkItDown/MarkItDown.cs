// MarkItDown/MarkItDown.cs

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MarkItDown.Converters;
using MarkItDown.Exceptions;
using MarkItDown.Helpers;
using MarkItDown.Models;

namespace MarkItDown
{
    public class MarkItDownConverter
    {
        private readonly HttpClient _httpClient;
        private readonly List<DocumentConverter> _pageConverters;

        public MarkItDownConverter(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();

            _pageConverters = new List<DocumentConverter>();

            // Register converters in order of priority
            RegisterPageConverter(new URLConverter(_httpClient));
            RegisterPageConverter(new ZipConverter());
            RegisterPageConverter(new PDFConverter());
            RegisterPageConverter(new DocxConverter());
            RegisterPageConverter(new XlsxConverter());
            RegisterPageConverter(new PptxConverter());
            RegisterPageConverter(new PlainTextConverter());
            RegisterPageConverter(new HtmlConverter());
            RegisterPageConverter(new WavConverter());
            RegisterPageConverter(new Mp3Converter());
            // Add other converters as needed
        }

        /// <summary>
        ///     Converts a local file or URL to Markdown.
        /// </summary>
        /// <param name="pathOrUrl">The local file path or URL.</param>
        /// <param name="options">Additional conversion options.</param>
        /// <returns>A DocumentConverterResult containing the conversion output.</returns>
        public async Task<DocumentConverterResult> ConvertLocalAsync(string pathOrUrl, ConversionOptions options = null)
        {
            options = options ?? new ConversionOptions();
            options.ParentConverters = _pageConverters;

            if (UrlHelper.IsValidUrl(pathOrUrl))
            {
                // Delegate to URLConverter
                var urlConverter = _pageConverters.OfType<URLConverter>()
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
                    if (converter.CanConvertFile(extension))
                    {
                        var result = await converter.ConvertAsync(pathOrUrl, options);
                        if (result != null)
                            return result;
                    }
            }

            throw new UnsupportedFormatException($"Unsupported input: {pathOrUrl}");
        }

        /// <summary>
        ///     Registers a new page converter.
        /// </summary>
        /// <param name="converter">The converter to register.</param>
        public void RegisterPageConverter(DocumentConverter converter)
        {
            _pageConverters.Insert(0, converter); // Higher priority converters are checked first
        }
    }
}