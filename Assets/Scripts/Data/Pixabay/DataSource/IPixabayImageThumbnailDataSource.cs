using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ImageSearch.Data.Pixabay.DataSource
{
    public interface IPixabayImageThumbnailDataSource
    {
        UniTask<byte[]> LoadAsync(Uri url, CancellationToken cancellationToken);
    }
}
