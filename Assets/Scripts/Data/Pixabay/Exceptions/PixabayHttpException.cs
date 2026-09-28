using System;

namespace ImageSearch.Data.Pixabay.Exceptions
{
    public sealed class PixabayHttpException : Exception
    {
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public PixabayHttpException(long statusCode, string responseBody)
            : base($"Pixabay returned HTTP status {statusCode}.")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
