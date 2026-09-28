using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Domain.Models;
using ImageSearch.Domain.Repositories;
using ImageSearch.UI;
using NUnit.Framework;
using UnityEngine;

namespace ImageSearch.Tests.EditMode.Presentation
{
    public sealed class SearchScreenPresenterTests
    {
        [TestCase(NetworkError.ConnectionFailure, "서버에 연결할 수 없어요. 잠시 후 다시 시도해 주세요.")]
        [TestCase(NetworkError.ServerError, "서버에 문제가 있어요. 잠시 후 다시 시도해 주세요.")]
        [TestCase(NetworkError.NoInternet, "인터넷 연결을 확인해 주세요.")]
        public void ErrorMessageVariesByNetworkError(NetworkError error, string expected)
        { Assert.That(SearchScreenPresenter.MessageFor(error), Is.EqualTo(expected)); }

        [Test] public async System.Threading.Tasks.Task EmptyResultShowsEmptyState()
        {
            var view = new RecordingView(); var presenter = new SearchScreenPresenter(new FakeRepository(new List<ImageItem>()), new FakeThumbnailRepository(), view);
            await presenter.SearchAsync("cats");
            Assert.That(view.Empty, Is.True); presenter.Dispose();
        }

        [Test] public async System.Threading.Tasks.Task NewSearchCancelsPreviousAndIgnoresItsLateResult()
        {
            var oldItems = new[] { CreateItem(1) };
            var newItems = new[] { CreateItem(2) };
            var repository = new DelayedFirstRepository(newItems);
            var view = new RecordingView();
            var presenter = new SearchScreenPresenter(repository, new FakeThumbnailRepository(), view);

            var oldSearch = presenter.SearchAsync("old");
            var oldToken = repository.FirstToken;
            await presenter.SearchAsync("new");
            repository.CompleteFirst(oldItems);
            await oldSearch;

            Assert.That(oldToken.IsCancellationRequested, Is.True);
            Assert.That(view.LastResults, Is.SameAs(newItems));
            presenter.Dispose();
        }

        private static ImageItem CreateItem(long id) => new ImageItem(id,
            new System.Uri("mock://thumb/" + id), 4.0 / 3.0,
            new System.Uri("https://example.invalid/image/" + id), new[] { "item" + id });

        private sealed class RecordingView : ISearchScreenView
        {
            public bool Empty;
            public IReadOnlyList<ImageItem> LastResults;
            public void ShowSearching() { }
            public void ShowResults(IReadOnlyList<ImageItem> items) { LastResults = items; }
            public void ShowEmptyResults() { Empty = true; }
            public void ShowError(string message) { }
            public void SetThumbnail(long imageId, Texture2D texture) { }
            public void ShowThumbnailPlaceholder(long imageId) { }
            public void ClearResults() { }
        }
        private sealed class FakeRepository : IImageSearchRepository
        {
            private readonly IReadOnlyList<ImageItem> _items;
            public FakeRepository(IReadOnlyList<ImageItem> items) => _items = items;
            public UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>> SearchAsync(string keyword, int page, int pageSize, CancellationToken cancellationToken)
                => UniTask.FromResult(Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(_items));
        }
        private sealed class FakeThumbnailRepository : IImageThumbnailRepository
        {
            public UniTask<Result<byte[], NetworkError>> LoadAsync(System.Uri thumbnailUrl, CancellationToken cancellationToken)
                => UniTask.FromResult(Result<byte[], NetworkError>.FromError(NetworkError.Unknown));
        }

        private sealed class DelayedFirstRepository : IImageSearchRepository
        {
            private readonly UniTaskCompletionSource<Result<IReadOnlyList<ImageItem>, NetworkError>> _first =
                new UniTaskCompletionSource<Result<IReadOnlyList<ImageItem>, NetworkError>>();
            private readonly IReadOnlyList<ImageItem> _second;
            private int _calls;
            public CancellationToken FirstToken { get; private set; }
            public DelayedFirstRepository(IReadOnlyList<ImageItem> second) => _second = second;
            public UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>> SearchAsync(string keyword, int page, int pageSize, CancellationToken cancellationToken)
            {
                if (_calls++ == 0) { FirstToken = cancellationToken; return _first.Task; }
                return UniTask.FromResult(Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(_second));
            }
            public void CompleteFirst(IReadOnlyList<ImageItem> items)
                => _first.TrySetResult(Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(items));
        }
    }
}
