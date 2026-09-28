using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Domain.Repositories;

namespace ImageSearch.Data.Pixabay.Repositories
{
    public sealed class PixabayImageThumbnailRepository : IImageThumbnailRepository
    {
        private readonly IPixabayImageThumbnailDataSource _dataSource;

        public PixabayImageThumbnailRepository(IPixabayImageThumbnailDataSource dataSource) =>
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

        public async UniTask<Result<byte[], NetworkError>> LoadAsync(Uri thumbnailUrl, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await _dataSource.LoadAsync(thumbnailUrl, cancellationToken);
                return Result<byte[], NetworkError>.FromSuccess(bytes);
            }
            catch (PixabayNetworkException exception)
            {
                switch (exception.Failure)
                {
                    case PixabayTransportFailure.NoInternet: return Result<byte[], NetworkError>.FromError(NetworkError.NoInternet);
                    case PixabayTransportFailure.Timeout: return Result<byte[], NetworkError>.FromError(NetworkError.NetworkTimeout);
                    default: return Result<byte[], NetworkError>.FromError(NetworkError.ConnectionFailure);
                }
            }
            catch (PixabayHttpException exception)
            {
                var error = exception.StatusCode == 404 ? NetworkError.NotFound :
                    exception.StatusCode == 429 ? NetworkError.RateLimited :
                    exception.StatusCode >= 500 ? NetworkError.ServerError : NetworkError.Unknown;
                return Result<byte[], NetworkError>.FromError(error);
            }
            catch (PixabayResponseException)
            {
                return Result<byte[], NetworkError>.FromError(NetworkError.Unknown);
            }
        }
    }
}
