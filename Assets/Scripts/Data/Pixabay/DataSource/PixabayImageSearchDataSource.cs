using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using UnityEngine;
using UnityEngine.Networking;

namespace ImageSearch.Data.Pixabay.DataSource
{
    public sealed class PixabayImageSearchDataSource : IPixabayImageSearchDataSource
    {
        private const string Endpoint = "https://pixabay.com/api/";
        private readonly string _apiKey;
        private readonly PixabaySearchCache _cache;
        private readonly PixabayRequestWindow _requestWindow = new PixabayRequestWindow();

        public PixabayImageSearchDataSource(string apiKey, string cacheDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("A Pixabay API key must be supplied from external configuration.", nameof(apiKey));
            _apiKey = apiKey;
            _cache = new PixabaySearchCache(cacheDirectory ?? Application.persistentDataPath);
        }

        public async UniTask<PixabaySearchResponseDto> SearchAsync(
            string keyword, int page, int pageSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = BuildRequestUrl(_apiKey, keyword, page, pageSize);
            if (_cache.TryRead(url, DateTime.UtcNow, out var cachedJson))
            {
                try { return ParseResponse(cachedJson); }
                catch (PixabayResponseException) { }
            }
            if (!_requestWindow.TryEnter(DateTime.UtcNow))
                throw new PixabayHttpException(429, "Local Pixabay request limit reached.");

            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                try
                {
                    await request.SendWebRequest().WithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    request.Abort();
                    throw;
                }

                if (request.result == UnityWebRequest.Result.ConnectionError)
                    throw MapTransportFailure(request.error);

                if (request.result == UnityWebRequest.Result.ProtocolError)
                    throw new PixabayHttpException(request.responseCode, request.downloadHandler?.text);

                if (request.result != UnityWebRequest.Result.Success)
                    throw new PixabayResponseException($"Pixabay request failed: {request.error}");

                var json = request.downloadHandler?.text;
                var response = ParseResponse(json);
                _cache.Store(url, json, DateTime.UtcNow);
                return response;
            }
        }

        internal static string BuildRequestUrl(string apiKey, string keyword, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key is required.", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(keyword))
                throw new ArgumentException("Keyword is required.", nameof(keyword));
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1 || pageSize > 200)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            return Endpoint + "?key=" + Uri.EscapeDataString(apiKey) +
                   "&q=" + Uri.EscapeDataString(keyword) +
                   "&image_type=photo&safesearch=true&page=" + page + "&per_page=" + pageSize;
        }

        internal static PixabaySearchResponseDto ParseResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new PixabayResponseException("Pixabay response body was empty.");

            try
            {
                var response = JsonUtility.FromJson<PixabaySearchResponseDto>(json);
                if (response == null || response.hits == null)
                    throw new PixabayResponseException("Pixabay response did not contain a hits array.");
                return response;
            }
            catch (PixabayResponseException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PixabayResponseException("Pixabay response JSON could not be parsed.", exception);
            }
        }

        private static PixabayNetworkException MapTransportFailure(string error)
        {
            if (!string.IsNullOrEmpty(error) && error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return new PixabayNetworkException(PixabayTransportFailure.Timeout);

            return new PixabayNetworkException(
                Application.internetReachability == NetworkReachability.NotReachable
                    ? PixabayTransportFailure.NoInternet
                    : PixabayTransportFailure.ConnectionFailure);
        }
    }
}
