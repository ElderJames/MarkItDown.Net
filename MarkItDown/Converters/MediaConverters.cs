// Converters/MediaConverters.cs

using System;
using System.IO;
using System.Threading.Tasks;
using MarkItDownSharp.Models;
using NAudio.Wave;
using File = System.IO.File;

namespace MarkItDownSharp.Converters
{
    public abstract class MediaConverter : DocumentConverter
    {
        protected string GetMetadata(string localPath)
        {
            try
            {
                var file = TagLib.File.Create(localPath);

                var metadata = $"Title: {file.Tag.Title}\n" +
                               $"Artist: {string.Join(", ", file.Tag.Performers)}\n" +
                               $"Album: {file.Tag.Album}\n" +
                               $"Duration: {file.Properties.Duration}\n";

                return metadata.Trim();
            }
            catch
            {
                return "Metadata could not be retrieved.";
            }
        }

        public override bool CanConvertUrl(string url)
        {
            return false;
        }

        public override bool CanConvertFile(string extension)
        {
            // To be overridden by subclasses
            return false;
        }
    }

    public class WavConverter : MediaConverter
    {
        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var markdown = "";

            // Extract metadata
            var metadata = GetMetadata(localPath);
            if (!string.IsNullOrEmpty(metadata)) markdown += metadata + "\n";

            // Transcribe audio
            var transcript = await TranscribeAudioAsync(localPath, options);
            if (!string.IsNullOrEmpty(transcript)) markdown += "\n### Audio Transcript:\n" + transcript + "\n";

            return new DocumentConverterResult
            {
                Title = null,
                TextContent = markdown.Trim()
            };
        }

        private async Task<string> TranscribeAudioAsync(string localPath, ConversionOptions options)
        {
            // Implement transcription logic using a speech recognition API or library
            // Placeholder: Return empty string
            await Task.CompletedTask;
            return "[Audio transcription not implemented]";
        }
    }

    public class Mp3Converter : MediaConverter
    {
        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            var markdown = "";

            // Extract metadata
            var metadata = GetMetadata(localPath);
            if (!string.IsNullOrEmpty(metadata)) markdown += metadata + "\n";

            // Convert MP3 to WAV for transcription
            var tempWavPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
            try
            {
                using (var reader = new Mp3FileReader(localPath))
                using (var writer = new WaveFileWriter(tempWavPath, reader.WaveFormat))
                {
                    reader.CopyTo(writer);
                }

                // Transcribe audio
                var transcript = await TranscribeAudioAsync(tempWavPath, options);
                if (!string.IsNullOrEmpty(transcript)) markdown += "\n### Audio Transcript:\n" + transcript + "\n";
            }
            catch
            {
                markdown += "\n### Audio Transcript:\nError. Could not transcribe this audio.\n";
            }
            finally
            {
                if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
            }

            return new DocumentConverterResult
            {
                Title = null,
                TextContent = markdown.Trim()
            };
        }

        private async Task<string> TranscribeAudioAsync(string localPath, ConversionOptions options)
        {
            // Implement transcription logic using a speech recognition API or library
            // Placeholder: Return empty string
            await Task.CompletedTask;
            return "[Audio transcription not implemented]";
        }
    }

    // Similarly, implement other MediaConverters like ImageConverter if needed
}