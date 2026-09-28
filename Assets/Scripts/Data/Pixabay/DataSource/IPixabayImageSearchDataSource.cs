using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.Dto;

namespace ImageSearch.Data.Pixabay.DataSource
{
    public interface IPixabayImageSearchDataSource
    {
        UniTask<PixabaySearchResponseDto> SearchAsync(
            string keyword, int page, int pageSize, CancellationToken cancellationToken);
    }
}
