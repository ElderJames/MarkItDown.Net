using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using MarkItDownSharp.Models;
using MarkItDownSharp.Services;

namespace MarkItDownSharp.Converters
{
    public class ImageConverter : DocumentConverter
    {
        private readonly IOcrService _ocrService;

        public ImageConverter(IOcrService ocrService)
        {
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> ExtractTextFromImage(byte[] imageData)
        {
            try
            {
                return await _ocrService.ExtractTextAsync(imageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR Error: {ex.Message}");
                return string.Empty;
            }
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var imageData = await File.ReadAllBytesAsync(localPath);
            var extractedText = await ExtractTextFromImage(imageData);

            return new DocumentConverterResult
            {
                Title = Path.GetFileNameWithoutExtension(localPath),
                TextContent = extractedText
            };
        }

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            var result = await ConvertAsync(pathOrUrl, options);
            return result != null ? new List<DocumentConverterResult> { result } : new List<DocumentConverterResult>();
        }
    }
}