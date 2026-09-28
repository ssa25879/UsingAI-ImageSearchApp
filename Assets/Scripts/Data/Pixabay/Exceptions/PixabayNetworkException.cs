using System;

namespace ImageSearch.Data.Pixabay.Exceptions
{
    public sealed class PixabayNetworkException : Exception
    {
        public PixabayTransportFailure Failure { get; }

        public PixabayNetworkException(PixabayTransportFailure failure, Exception innerException = null)
            : base($"Pixabay transport failed: {failure}.", innerException)
        {
            Failure = failure;
        }
    }
}
