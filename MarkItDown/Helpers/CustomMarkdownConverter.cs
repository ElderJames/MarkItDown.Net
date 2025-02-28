// Helpers/CustomMarkdownConverter.cs

using HtmlAgilityPack;

using ReverseMarkdown;

namespace MarkItDownSharp.Helpers
{
    public class CustomMarkdownConverter
    {

        /// <summary>
        ///     Converts an HtmlDocument to Markdown.
        /// </summary>
        /// <param name="html">The HTML string.</param>
        /// <returns>Markdown string.</returns>
        public string ConvertToMarkdown(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script and style tags
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? new HtmlNodeCollection(null))
                node.Remove();


            // Select all <img> tags
            var imageNodes = doc.DocumentNode.SelectNodes("//img");

            if (imageNodes != null)
                foreach (var img in imageNodes)
                {
                    var src = img.GetAttributeValue("src", string.Empty);

                    if (src.StartsWith("data:image/"))
                        // Remove the <img> node from the HTML
                        img.Remove();
                }

            // Save or display the cleaned HTML
            var cleanedHtml = doc.DocumentNode.OuterHtml;


            var config = new Config
            {
                // Include the unknown tag completely in the result (default as well)
                UnknownTags = Config.UnknownTagsOption.Drop,
                // generate GitHub flavoured markdown, supported for BR, PRE and table tags
                GithubFlavored = true,
                // will ignore all comments
                RemoveComments = true,
                // remove markdown output for links where appropriate
                SmartHrefHandling = true,
                CleanupUnnecessarySpaces = true
            };

            var converter = new Converter(config);

            var markdown = converter.Convert(cleanedHtml);


            // You can enhance this by handling links, images, headers, etc., as per your requirements
            return markdown;
        }
    }
}