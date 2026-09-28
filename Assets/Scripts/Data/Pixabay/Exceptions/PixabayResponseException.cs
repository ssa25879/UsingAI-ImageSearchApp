using System;

namespace ImageSearch.Data.Pixabay.Exceptions
{
    public sealed class PixabayResponseException : Exception
    {
        public PixabayResponseException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}
