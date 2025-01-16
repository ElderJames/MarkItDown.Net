// Helpers/PathHelper.cs

using System;
using System.IO;

namespace MarkItDown.Helpers
{
    public static class PathHelper
    {
        /// <summary>
        ///     Computes the relative path from one path to another.
        /// </summary>
        /// <param name="relativeTo">The base path.</param>
        /// <param name="path">The target path.</param>
        /// <returns>The relative path from the base path to the target path.</returns>
        public static string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo))
                throw new ArgumentNullException(nameof(relativeTo));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var uri1 = new Uri(AppendDirectorySeparatorChar(relativeTo));
            var uri2 = new Uri(path);
            var relativeUri = uri1.MakeRelativeUri(uri2);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString())
                .Replace('/', Path.DirectorySeparatorChar);
            return relativePath;
        }

        /// <summary>
        ///     Ensures the path ends with a directory separator character.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>The path ending with a directory separator character.</returns>
        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }
    }
}