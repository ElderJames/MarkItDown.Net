// Exceptions/UnsupportedFormatException.cs

using System;

namespace MarkItDownSharp.Exceptions
{
    public class UnsupportedFormatException : Exception
    {
        public UnsupportedFormatException()
        {
        }

        public UnsupportedFormatException(string message) : base(message)
        {
        }

        public UnsupportedFormatException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}