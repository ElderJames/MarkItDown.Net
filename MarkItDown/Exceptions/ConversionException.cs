// Exceptions/FileConversionException.cs

using System;

namespace MarkItDown.Exceptions
{
    public class ConversionException : Exception
    {
        public ConversionException()
        {
        }

        public ConversionException(string message) : base(message)
        {
        }

        public ConversionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}