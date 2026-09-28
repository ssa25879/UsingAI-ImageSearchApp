using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Domain.Models;
using ImageSearch.Domain.Repositories;
using UnityEngine;

namespace ImageSearch.UI
{
    public interface ISearchScreenView
    {
        void ShowSearching();
        void ShowResults(IReadOnlyList<ImageItem> items);
        void ShowEmptyResults();
        void ShowError(string message);
        void SetThumbnail(long imageId, Texture2D texture);
        void ShowThumbnailPlaceholder(long imageId);
        void ClearResults();
    }

    public sealed class SearchScreenPresenter : IDisposable
    {
        private readonly IImageSearchRepository _repository;
        private readonly IImageThumbnailRepository _thumbnailRepository;
        private readonly ISearchScreenView _view;
        private CancellationTokenSource _searchCts;
        private int _operation;

        public SearchScreenPresenter(IImageSearchRepository repository, IImageThumbnailRepository thumbnailRepository, ISearchScreenView view)
        { _repository = repository ?? throw new ArgumentNullException(nameof(repository)); _thumbnailRepository = thumbnailRepository ?? throw new ArgumentNullException(nameof(thumbnailRepository)); _view = view ?? throw new ArgumentNullException(nameof(view)); }

        public async UniTask SearchAsync(string keyword)
        {
            _searchCts?.Cancel(); _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var operation = ++_operation;
            _view.ShowSearching();
            _view.ClearResults();
            try
            {
                var result = await _repository.SearchAsync(keyword, 1, 20, token);
                if (operation != _operation) return;
                switch (result)
                {
                    case Result<IReadOnlyList<ImageItem>, NetworkError>.Success success:
                        if (success.Data.Count == 0) _view.ShowEmptyResults();
                        else
                        {
                            _view.ShowResults(success.Data);
                            foreach (var item in success.Data) LoadThumbnailAsync(item, operation, token).Forget();
                        }
                        break;
                    case Result<IReadOnlyList<ImageItem>, NetworkError>.Error error:
                        _view.ShowError(MessageFor(error.Value));
                        break;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid LoadThumbnailAsync(ImageItem item, int operation, CancellationToken token)
        {
            try
            {
                var result = await _thumbnailRepository.LoadAsync(item.ThumbnailUrl, token);
                if (token.IsCancellationRequested || operation != _operation) return;
                switch (result)
                {
                    case Result<byte[], NetworkError>.Success success:
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (ImageConversion.LoadImage(texture, success.Data)) _view.SetThumbnail(item.Id, texture);
                        else { UnityEngine.Object.Destroy(texture); _view.ShowThumbnailPlaceholder(item.Id); }
                        break;
                    case Result<byte[], NetworkError>.Error:
                        _view.ShowThumbnailPlaceholder(item.Id);
                        break;
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        { _operation++; _searchCts?.Cancel(); _searchCts?.Dispose(); _searchCts = null; }

        public static string MessageFor(NetworkError error)
        {
            switch (error)
            {
                case NetworkError.NoInternet: return "인터넷 연결을 확인해 주세요.";
                case NetworkError.NetworkTimeout: return "응답 시간이 초과됐어요. 다시 시도해 주세요.";
                case NetworkError.ConnectionFailure: return "서버에 연결할 수 없어요. 잠시 후 다시 시도해 주세요.";
                case NetworkError.BadRequest: return "검색어를 확인해 주세요.";
                case NetworkError.Unauthorized: return "인증이 필요해요.";
                case NetworkError.Forbidden: return "요청 권한이 없어요.";
                case NetworkError.NotFound: return "검색 결과를 찾을 수 없어요.";
                case NetworkError.RateLimited: return "요청이 많아요. 잠시 후 다시 시도해 주세요.";
                case NetworkError.ServerError: return "서버에 문제가 있어요. 잠시 후 다시 시도해 주세요.";
                default: return "알 수 없는 오류가 발생했어요.";
            }
        }
    }
}
