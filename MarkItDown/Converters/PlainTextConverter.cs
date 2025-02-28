// Converters/PlainTextConverter.cs

using System;
using System.IO;
using System.Threading.Tasks;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public class PlainTextConverter : DocumentConverter
    {
        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var textContent = await Task.Run(() => File.ReadAllText(localPath));
            return new DocumentConverterResult
            {
                Title = null,
                TextContent = textContent
            };
        }
    }
}