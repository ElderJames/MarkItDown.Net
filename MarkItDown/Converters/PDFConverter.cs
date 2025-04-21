// Converters/PdfConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkItDownSharp.Models;
using MarkItDownSharp.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Images;

namespace MarkItDownSharp.Converters
{
    public class PdfConverter : DocumentConverter
    {
        private readonly IOcrService _ocrService;

        public PdfConverter(IOcrService ocrService)
        {
            
            _ocrService = ocrService;
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private class ContentItem
        {
            public double Y { get; set; }
            public string Text { get; set; }
            public bool IsImage { get; set; }
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"File not found: {localPath}");

            var markdownBuilder = new StringBuilder();
            var title = Path.GetFileNameWithoutExtension(localPath);

            using (var document = PdfDocument.Open(localPath))
            {
                var pages = document.GetPages().ToList();
                var isFirstLineExtracted = false;

                foreach (var page in pages)
                {
                    var contentItems = new List<ContentItem>();
                    var words = page.GetWords().ToList();

                    // 处理标题（如果还没有提取）
                    if (!isFirstLineExtracted && words.Any())
                    {
                        var lines = GroupWordsIntoLines(words);
                        if (lines.Any())
                        {
                            var firstLine = lines.First();
                            title = string.Join(" ", firstLine.Words.Select(w => w.Text)).Trim();
                            isFirstLineExtracted = true;
                        }
                    }

                    // 处理文本内容
                    var linesInPage = GroupWordsIntoLines(words);
                    foreach (var line in linesInPage)
                    {
                        var sortedWords = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                        var lineText = string.Join(" ", sortedWords.Select(w => w.Text)).Trim();
                        contentItems.Add(new ContentItem
                        {
                            Y = line.Y,
                            Text = lineText,
                            IsImage = false
                        });
                    }

                    // 处理图片内容
                    var images = page.GetImages().ToList();
                    foreach (var image in images)
                    {
                        try
                        {
                            var imageData = image.RawBytes.ToArray();
                            var extractedText = await _ocrService.ExtractTextAsync(imageData);
                            if (!string.IsNullOrEmpty(extractedText))
                            {
                                // 获取图片在页面中的位置
                                var imageY = image.Bounds.Bottom;
                                contentItems.Add(new ContentItem
                                {
                                    Y = imageY,
                                    Text = $"\n[Image OCR Result]:\n{extractedText}\n",
                                    IsImage = true
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing image in PDF: {ex.Message}");
                        }
                    }

                    // 按Y坐标排序所有内容（从上到下）
                    var sortedContent = contentItems.OrderByDescending(item => item.Y);
                    foreach (var item in sortedContent)
                    {
                        markdownBuilder.AppendLine(item.Text);
                    }

                    markdownBuilder.AppendLine();
                }
            }

            var finalMarkdown = markdownBuilder.ToString().Trim();

            var result = new DocumentConverterResult
            {
                Title = title,
                TextContent = finalMarkdown
            };

            return result;
        }

        public override async Task<List<DocumentConverterResult>> ConvertToListAsync(string pathOrUrl, ConversionOptions options)
        {
            var result = await ConvertAsync(pathOrUrl, options);
            return result != null ? new List<DocumentConverterResult> { result } : new List<DocumentConverterResult>();
        }

        /// <summary>
        ///     Groups words into lines based on their vertical (Y) positions.
        /// </summary>
        private List<Line> GroupWordsIntoLines(List<Word> words)
        {
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();
            var lines = new List<Line>();
            var lineTolerance = 2.0; // Adjust as needed for vertical grouping

            foreach (var word in sortedWords)
            {
                var addedToLine = false;

                foreach (var line in lines)
                    if (Math.Abs(line.Y - word.BoundingBox.Bottom) <= lineTolerance)
                    {
                        line.Words.Add(word);
                        addedToLine = true;
                        break;
                    }

                if (!addedToLine)
                    lines.Add(new Line
                    {
                        Y = word.BoundingBox.Bottom,
                        Words = new List<Word> { word }
                    });
            }

            return lines;
        }

        /// <summary>
        ///     Represents a line of text composed of multiple words.
        /// </summary>
        private class Line
        {
            public double Y { get; set; }
            public List<Word> Words { get; set; }
        }
    }
}