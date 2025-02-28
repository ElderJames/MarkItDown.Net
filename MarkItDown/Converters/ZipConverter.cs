// Converters/ZipConverter.cs

using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MarkItDownSharp.Helpers;
using MarkItDownSharp.Models;

namespace MarkItDownSharp.Converters
{
    public class ZipConverter : DocumentConverter
    {
        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            if (options.ParentConverters == null || options.ParentConverters.Count == 0)
                return new DocumentConverterResult
                {
                    Title = null,
                    TextContent = $"[ERROR] No converters available to process zip contents from: {localPath}"
                };

            var extractedFolderName = $"extracted_{Path.GetFileNameWithoutExtension(localPath)}_zip";
            var destinationPath = Path.Combine(Path.GetDirectoryName(localPath), extractedFolderName);

            // Prevent path traversal
            if (!destinationPath.StartsWith(Path.GetDirectoryName(localPath), StringComparison.OrdinalIgnoreCase))
                return new DocumentConverterResult
                {
                    Title = null,
                    TextContent = "[ERROR] Invalid zip file path."
                };

            var markdownContent = $"Content from the zip file `{Path.GetFileName(localPath)}`:\n\n";

            try
            {
                // Extract ZIP
                Directory.CreateDirectory(destinationPath);
                using (var fs = File.OpenRead(localPath))
                using (var zf = new ZipFile(fs))
                {
                    foreach (ZipEntry zipEntry in zf)
                    {
                        if (!zipEntry.IsFile)
                            continue;

                        var entryFileName = zipEntry.Name;
                        var buffer = new byte[4096];
                        var zipStream = zf.GetInputStream(zipEntry);

                        var fullZipToPath = Path.Combine(destinationPath, entryFileName);
                        var directoryName = Path.GetDirectoryName(fullZipToPath);
                        if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

                        using (var streamWriter = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }

                // Process extracted files
                foreach (var filePath in Directory.EnumerateFiles(destinationPath, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = PathHelper.GetRelativePath(destinationPath, filePath);
                    var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                    // Use existing CanConvertFile method
                    var converter = options.ParentConverters.Find(c => c.CanConvertFile(fileExtension));
                    if (converter != null)
                    {
                        var newOptions = new ConversionOptions
                        {
                            FileExtension = fileExtension,
                            ParentConverters = options.ParentConverters,
                            HttpClient = options.HttpClient,
                            LlmClient = options.LlmClient,
                            LlmModel = options.LlmModel,
                            StyleMap = options.StyleMap,
                            CleanupExtracted = options.CleanupExtracted
                        };

                        var result = await converter.ConvertAsync(filePath, newOptions);
                        if (result != null) markdownContent += $"## File: {relativePath}\n\n{result.TextContent}\n\n";
                    }
                    else
                    {
                        markdownContent +=
                            $"### Unsupported File: {relativePath}\n\n_No converter available for this file type._\n\n";
                    }
                }

                // Cleanup
                if (options.CleanupExtracted) Directory.Delete(destinationPath, true);

                return new DocumentConverterResult
                {
                    Title = null,
                    TextContent = markdownContent.Trim()
                };
            }
            catch (Exception ex)
            {
                return new DocumentConverterResult
                {
                    Title = null,
                    TextContent = $"[ERROR] Failed to process zip file {localPath}: {ex.Message}"
                };
            }
        }
    }
}