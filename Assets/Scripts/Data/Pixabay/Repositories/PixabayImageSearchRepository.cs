using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Data.Pixabay.Mapper;
using ImageSearch.Domain.Models;
using ImageSearch.Domain.Repositories;

namespace ImageSearch.Data.Pixabay.Repositories
{
    public sealed class PixabayImageSearchRepository : IImageSearchRepository
    {
        private readonly IPixabayImageSearchDataSource _dataSource;
        private readonly PixabayImageSearchMapper _mapper;

        public PixabayImageSearchRepository(
            IPixabayImageSearchDataSource dataSource, PixabayImageSearchMapper mapper)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>> SearchAsync(
            string keyword, int page, int pageSize, CancellationToken cancellationToken)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize));

            if (string.IsNullOrWhiteSpace(keyword))
                return Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(Array.Empty<ImageItem>());

            try
            {
                var response = await _dataSource.SearchAsync(keyword.Trim(), page, pageSize, cancellationToken);
                if (response?.hits == null)
                    return Result<IReadOnlyList<ImageItem>, NetworkError>.FromError(NetworkError.Unknown);

                var items = new List<ImageItem>(response.hits.Count);
                foreach (PixabayHitDto hit in response.hits)
                    items.Add(_mapper.ToModel(hit));

                return Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(items.AsReadOnly());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (PixabayNetworkException exception)
            {
                return Result<IReadOnlyList<ImageItem>, NetworkError>.FromError(MapTransportFailure(exception.Failure));
            }
            catch (PixabayHttpException exception)
            {
                return Result<IReadOnlyList<ImageItem>, NetworkError>.FromError(MapHttpStatus(exception.StatusCode));
            }
            catch (PixabayResponseException)
            {
                return Result<IReadOnlyList<ImageItem>, NetworkError>.FromError(NetworkError.Unknown);
            }
        }

        private static NetworkError MapTransportFailure(PixabayTransportFailure failure)
        {
            switch (failure)
            {
                case PixabayTransportFailure.NoInternet: return NetworkError.NoInternet;
                case PixabayTransportFailure.Timeout: return NetworkError.NetworkTimeout;
                case PixabayTransportFailure.ConnectionFailure: return NetworkError.ConnectionFailure;
                default: return NetworkError.Unknown;
            }
        }

        private static NetworkError MapHttpStatus(long statusCode)
        {
            switch (statusCode)
            {
                case 400: return NetworkError.BadRequest;
                case 401: return NetworkError.Unauthorized;
                case 403: return NetworkError.Forbidden;
                case 404: return NetworkError.NotFound;
                case 429: return NetworkError.RateLimited;
                default: return statusCode >= 500 && statusCode <= 599
                    ? NetworkError.ServerError
                    : NetworkError.Unknown;
            }
        }
    }
}
