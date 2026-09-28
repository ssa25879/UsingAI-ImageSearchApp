using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Domain.Models;

namespace ImageSearch.Domain.Repositories
{
    public interface IImageSearchRepository
    {
        UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>> SearchAsync(
            string keyword, int page, int pageSize, CancellationToken cancellationToken);
    }
}
