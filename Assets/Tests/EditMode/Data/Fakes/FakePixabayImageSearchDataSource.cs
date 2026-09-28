using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Dto;

namespace ImageSearch.Tests.EditMode.Data.Fakes
{
    internal sealed class FakePixabayImageSearchDataSource : IPixabayImageSearchDataSource
    {
        public PixabaySearchResponseDto Response { get; set; }
        public Exception ExceptionToThrow { get; set; }
        public int CallCount { get; private set; }
        public string LastKeyword { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public UniTask<PixabaySearchResponseDto> SearchAsync(
            string keyword, int page, int pageSize, CancellationToken cancellationToken)
        {
            CallCount++;
            LastKeyword = keyword;
            LastPage = page;
            LastPageSize = pageSize;
            LastCancellationToken = cancellationToken;

            if (ExceptionToThrow != null)
                return UniTask.FromException<PixabaySearchResponseDto>(ExceptionToThrow);

            return UniTask.FromResult(Response);
        }
    }
}
