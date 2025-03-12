// Converters/DocumentConverter.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public abstract class DocumentConverter
    {
        /// <summary>
        /// Converts the document at the given path or URL to Markdown.
        /// </summary>
        /// <param name="pathOrUrl">The local file path or URL of the document.</param>
        /// <param name="options">Additional options for conversion.</param>
        /// <returns>A DocumentConverterResult containing the conversion output.</returns>
        public abstract Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options);

        /// <summary>
        /// Converts the document to a list of DocumentConverterResult.
        /// The default implementation calls ConvertAsync and wraps its result in a list.
        /// Converters that support multi‑page documents should override this method.
        /// </summary>
        public abstract Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options);
        

        /// <summary>
        /// Determines if the converter can handle the given URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if it can handle the URL; otherwise, false.</returns>
        public virtual bool CanConvertUrl(string url)
        {
            return false;
        }

        /// <summary>
        /// Determines if the converter can handle the given file extension.
        /// </summary>
        /// <param name="extension">The file extension (e.g., ".txt").</param>
        /// <returns>True if it can handle the extension; otherwise, false.</returns>
        public virtual bool CanConvertFile(string extension)
        {
            return false;
        }
    }
}