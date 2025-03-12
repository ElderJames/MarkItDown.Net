// Converters/PdfConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkItDownSharp.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MarkItDownSharp.Converters
{
    public class PdfConverter : DocumentConverter
    {
        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"File not found: {localPath}");

            var markdownBuilder = new StringBuilder();
            var title = Path.GetFileNameWithoutExtension(localPath); // Default title: document name

            using (var document = PdfDocument.Open(localPath))
            {
                var pages = document.GetPages().ToList();
                var isFirstLineExtracted = false; // Flag to check if the first line has been extracted

                foreach (var page in pages)
                {
                    var words = page.GetWords().ToList();

                    if (!isFirstLineExtracted && words.Any())
                    {
                        // Group words into lines first
                        var lines = GroupWordsIntoLines(words);

                        if (lines.Any())
                        {
                            // Extract the first line as the title
                            var firstLine = lines.First();
                            title = string.Join(" ", firstLine.Words.Select(w => w.Text)).Trim();
                            isFirstLineExtracted = true;
                        }
                    }

                    // Group words into lines for the entire page
                    var linesInPage = GroupWordsIntoLines(words);

                    // Sort lines by Y (top to bottom)
                    var sortedLines = linesInPage.OrderByDescending(l => l.Y).ToList();

                    // Append lines to the markdown content
                    foreach (var line in sortedLines)
                    {
                        // Sort words in the line by their X positions (left to right)
                        var sortedWords = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                        var lineText = string.Join(" ", sortedWords.Select(w => w.Text)).Trim();
                        markdownBuilder.AppendLine(lineText);
                    }

                    markdownBuilder.AppendLine(); // Add a newline after each page for separation
                }
            }

            // Finalize the markdown content
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