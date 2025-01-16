// Converters/YouTubeConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MarkItDown.Helpers;
using MarkItDown.Models;
using Newtonsoft.Json.Linq;
using YoutubeExplode;

namespace MarkItDown.Converters
{
    public class YouTubeConverter : DocumentConverter
    {
        private readonly HttpClient _httpClient;
        private readonly CustomMarkdownConverter _markdownConverter;
        private readonly YoutubeClient _youtubeClient;

        public YouTubeConverter(HttpClient httpClient)
        {
            _markdownConverter = new CustomMarkdownConverter();
            _httpClient = httpClient;
            _youtubeClient = new YoutubeClient();
        }

        public override bool CanConvertUrl(string url)
        {
            return Regex.IsMatch(url, @"^(https?://)?(www\.)?(youtube\.com|youtu\.be)/");
        }

        public override bool CanConvertFile(string extension)
        {
            return false;
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string pathOrUrl, ConversionOptions options)
        {
            if (!CanConvertUrl(options.Url)) return null;

            var htmlContent = await Task.Run(() => File.ReadAllText(pathOrUrl));
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var metadata = await GetMetadataAsync(doc, options.Url);
            var title = metadata.ContainsKey("title")
                ? metadata["title"]
                : doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();

            var markdown = new StringBuilder();

            // Add title
            markdown.AppendLine($"# {title}\n");

            // Add metadata section
            markdown.AppendLine("## Video Information\n");

            // Add metadata in a consistent order
            var metadataOrder = new[]
            {
                ("channel", "Channel"),
                ("uploadDate", "Upload Date"),
                ("duration", "Duration"),
                ("views", "Views"),
                ("likes", "Likes"),
                ("keywords", "Keywords")
            };

            foreach (var (key, label) in metadataOrder)
                if (metadata.ContainsKey(key) && !string.IsNullOrWhiteSpace(metadata[key]))
                    markdown.AppendLine($"- **{label}:** {metadata[key]}");

            markdown.AppendLine();

            // Add URL
            if (!string.IsNullOrEmpty(options.Url)) markdown.AppendLine($"**Video URL:** {options.Url}\n");

            // Add description if available
            if (metadata.ContainsKey("description") && !string.IsNullOrWhiteSpace(metadata["description"]))
            {
                markdown.AppendLine("## Description\n");
                markdown.AppendLine(metadata["description"]);
                markdown.AppendLine();
            }

            // Add transcript if available and requested

            var transcript = await GetYouTubeTranscriptAsync(options.Url, options);
            if (!string.IsNullOrEmpty(transcript) && transcript != "No transcript available" &&
                !transcript.StartsWith("Error"))
            {
                markdown.AppendLine("## Transcript\n");
                markdown.AppendLine(transcript);
            }

            return new DocumentConverterResult
            {
                Title = title,
                TextContent = markdown.ToString()
            };
        }

        private async Task<Dictionary<string, string>> GetMetadataAsync(HtmlDocument doc, string url)
        {
            var metadata = new Dictionary<string, string>();

            // First try the original metadata extraction
            foreach (var meta in doc.DocumentNode.SelectNodes("//meta") ?? new HtmlNodeCollection(null))
            foreach (var attribute in meta.Attributes)
                if (attribute.Name == "itemprop" || attribute.Name == "property" || attribute.Name == "name")
                {
                    metadata[attribute.Name] = meta.GetAttributeValue("content", "");
                    break;
                }

            // Try to extract from ytInitialData
            try
            {
                var scripts = doc.DocumentNode.SelectNodes("//script");
                if (scripts != null)
                    foreach (var script in scripts)
                        if (script.InnerText.Contains("ytInitialData"))
                        {
                            var match = Regex.Match(script.InnerText, @"ytInitialData\s*=\s*({.*});");
                            if (match.Success)
                            {
                                var json = match.Groups[1].Value;
                                var jObject = JObject.Parse(json);
                                var desc = FindKey(jObject, "attributedDescriptionBodyText");
                                if (desc != null && desc["content"] != null)
                                    metadata["description"] = desc["content"].ToString();
                            }

                            break;
                        }
            }
            catch
            {
                // Ignore parsing errors
            }

            // Fallback: Use YoutubeExplode to get additional metadata
            try
            {
                var videoId = ExtractVideoId(url);
                if (!string.IsNullOrEmpty(videoId))
                {
                    var video = await _youtubeClient.Videos.GetAsync(videoId);

                    // Add or update metadata with YoutubeExplode data
                    metadata["title"] = video.Title;
                    metadata["description"] = video.Description;
                    metadata["views"] = video.Engagement.ViewCount.ToString("N0");
                    metadata["channel"] = video.Author.ChannelTitle;
                    metadata["uploadDate"] = video.UploadDate.ToString("yyyy-MM-dd");

                    if (video.Duration != null)
                        metadata["duration"] = video.Duration.ToString();
                    else
                        metadata["duration"] = "Unknown";

                    try
                    {
                        var likeCount = video.Engagement.LikeCount;
                        metadata["likes"] = likeCount.ToString("N0");
                    }
                    catch
                    {
                        metadata["likes"] = "Hidden";
                    }
                }
            }
            catch
            {
                // Fallback failed, continue with existing metadata
            }

            return metadata;
        }

        private async Task<string> GetYouTubeTranscriptAsync(string url, ConversionOptions options)
        {
            try
            {
                var videoId = ExtractVideoId(url);
                if (string.IsNullOrEmpty(videoId))
                    return "Unable to extract video ID";

                var trackManifest = await _youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoId);
                var track = trackManifest.GetByLanguage("en");
                if (track == null && trackManifest.Tracks.Count() > 0) track = trackManifest.Tracks.First();
                if (track == null)
                    return "No transcript available";

                var transcript = await _youtubeClient.Videos.ClosedCaptions.GetAsync(track);
                var transcriptText = string.Join(" ", transcript.Captions.Select(caption => caption.Text));

                // Replace newline characters with a space
                transcriptText = transcriptText.Replace("\n", " ").Replace("\r", " ");

                // Replace multiple spaces with a single space
                transcriptText = Regex.Replace(transcriptText, @"\s+", " ");

                return transcriptText;
            }
            catch (Exception ex)
            {
                return $"Error retrieving transcript: {ex.Message}";
            }
        }

        private string ExtractVideoId(string url)
        {
            var youtubeIdRegex =
                new Regex(
                    @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
                    RegexOptions.IgnoreCase);
            var match = youtubeIdRegex.Match(url);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return timeSpan.Hours > 0
                ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        private JObject FindKey(JToken json, string key)
        {
            if (json is JProperty property)
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return property.Value as JObject;

            foreach (var child in json.Children())
            {
                var result = FindKey(child, key);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}