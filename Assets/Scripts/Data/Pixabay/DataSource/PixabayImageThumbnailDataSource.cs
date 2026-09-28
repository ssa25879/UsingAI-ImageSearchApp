using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.Exceptions;
using UnityEngine;
using UnityEngine.Networking;

namespace ImageSearch.Data.Pixabay.DataSource
{
    public sealed class PixabayImageThumbnailDataSource : IPixabayImageThumbnailDataSource
    {
        public async UniTask<byte[]> LoadAsync(Uri url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                    throw new PixabayNetworkException(
                        !string.IsNullOrEmpty(request.error) &&
                        request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                            ? PixabayTransportFailure.Timeout
                            : Application.internetReachability == NetworkReachability.NotReachable
                                ? PixabayTransportFailure.NoInternet
                                : PixabayTransportFailure.ConnectionFailure);
                if (request.result == UnityWebRequest.Result.ProtocolError)
                    throw new PixabayHttpException(request.responseCode, null);
                var bytes = request.downloadHandler?.data;
                if (request.result != UnityWebRequest.Result.Success || bytes == null || bytes.Length == 0)
                    throw new PixabayResponseException("Thumbnail response was empty or invalid.");
                return bytes;
            }
        }
    }
}
