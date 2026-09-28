using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;

namespace ImageSearch.Domain.Repositories
{
    public interface IImageThumbnailRepository
    {
        UniTask<Result<byte[], NetworkError>> LoadAsync(Uri thumbnailUrl, CancellationToken cancellationToken);
    }
}
