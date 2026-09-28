namespace ImageSearch.Core
{
    public enum NetworkError
    {
        NoInternet,
        NetworkTimeout,
        ConnectionFailure,
        BadRequest,
        Unauthorized,
        Forbidden,
        NotFound,
        RateLimited,
        ServerError,
        Unknown
    }
}
