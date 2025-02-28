// Helpers/UrlHelper.cs

using System;

namespace MarkItDownSharp.Helpers
{
    public static class UrlHelper
    {
        /// <summary>
        ///     Determines if the given input string is a valid HTTP or HTTPS URL.
        /// </summary>
        /// <param name="input">The input string to validate.</param>
        /// <returns>True if the input is a URL; otherwise, false.</returns>
        public static bool IsValidUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}