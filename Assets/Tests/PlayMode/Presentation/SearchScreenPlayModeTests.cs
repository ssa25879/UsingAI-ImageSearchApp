#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Domain.Models;
using ImageSearch.Domain.Repositories;
using ImageSearch.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ImageSearch.Tests.PlayMode.Presentation
{
    public sealed class SearchScreenPlayModeTests
    {
        private const string EmptyMessage = "검색 결과가 없어요. 다른 키워드로 찾아보세요.";
        private const float TimeoutSeconds = 5f;
        private GameObject _host;
        private PanelSettings _panelSettings;
        private UIDocument _document;
        private RecordingSearchRepository _repository;
        private TextField _searchField;
        private Button _searchButton;
        private Label _statusLabel;
        private Label _resultCount;
        private VisualElement _resultsGrid;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var screen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/ImageSearch/Runtime/SearchScreen.uxml");
            var card = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/ImageSearch/Runtime/ResultCard.uxml");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/ImageSearch/Settings/ImageSearchPanelSettings.asset");
            Assert.That(screen, Is.Not.Null);
            Assert.That(card, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);

            _repository = new RecordingSearchRepository();
            _host = new GameObject("Search screen PlayMode test");
            _host.SetActive(false);
            _panelSettings = UnityEngine.Object.Instantiate(panel);
            _document = _host.AddComponent<UIDocument>();
            _document.panelSettings = _panelSettings;
            _document.visualTreeAsset = screen;
            var controller = _host.AddComponent<SearchScreenPreviewController>();
            typeof(SearchScreenPreviewController).GetField("_resultCardTemplate", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, card);
            controller.Initialize(new SearchScreenPresenter(_repository, new OfflineThumbnailRepository(), controller));
            _host.SetActive(true);

            yield return WaitFor(() => _document.rootVisualElement.Q<Button>("searchButton") != null &&
                _document.rootVisualElement.Q<Label>("statusLabel").text == EmptyMessage, "screen initialization");
            var root = _document.rootVisualElement;
            _searchField = root.Q<TextField>("searchField");
            _searchButton = root.Q<Button>("searchButton");
            _statusLabel = root.Q<Label>("statusLabel");
            _resultCount = root.Q<Label>("resultCount");
            _resultsGrid = root.Q<VisualElement>("resultsGrid");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) UnityEngine.Object.Destroy(_host);
            if (_panelSettings != null) UnityEngine.Object.Destroy(_panelSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstScreenShowsGuidanceWithoutResults()
        {
            Assert.That(_statusLabel.text, Is.EqualTo(EmptyMessage));
            Assert.That(_resultCount.text, Is.EqualTo("RESULTS 00"));
            Assert.That(CardCount, Is.Zero);
            Assert.That(_repository.CallCount, Is.Zero);
            yield break;
        }

        [UnityTest]
        public IEnumerator SuccessfulSearchShowsCountAndFirstCardText()
        {
            _repository.EnqueueSuccess(Item(1, "고양이"), Item(2, "꽃"), Item(3, "바다"));
            Search("고양이");
            yield return WaitFor(() => CardCount == 3, "three search cards");
            Assert.That(_resultCount.text, Is.EqualTo("RESULTS 03"));
            Assert.That(_resultsGrid.Q<Label>("cardTitle").text, Is.EqualTo("고양이"));
        }

        [UnityTest]
        public IEnumerator EmptySearchShowsEmptyMessage()
        {
            _repository.EnqueueSuccess();
            Search("없는 이미지");
            yield return WaitFor(() => _repository.CallCount == 1 && _statusLabel.text == EmptyMessage, "empty result");
            Assert.That(CardCount, Is.Zero);
            Assert.That(_resultCount.text, Is.EqualTo("RESULTS 00"));
        }

        [UnityTest]
        public IEnumerator ConnectionFailureShowsConnectionMessage()
        {
            yield return ErrorShowsMessage(NetworkError.ConnectionFailure, "서버에 연결할 수 없어요. 잠시 후 다시 시도해 주세요.");
        }

        [UnityTest]
        public IEnumerator ServerErrorShowsServerMessage()
        {
            yield return ErrorShowsMessage(NetworkError.ServerError, "서버에 문제가 있어요. 잠시 후 다시 시도해 주세요.");
        }

        [UnityTest]
        public IEnumerator BlankKeywordDoesNotRequestOrChangeScreen()
        {
            Search("   ");
            yield return null;
            Assert.That(_repository.CallCount, Is.Zero);
            Assert.That(_statusLabel.text, Is.EqualTo(EmptyMessage));
            Assert.That(CardCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator NewSearchReplacesPreviousCards()
        {
            _repository.EnqueueSuccess(Item(1, "이전 A"), Item(2, "이전 B"));
            _repository.EnqueueSuccess(Item(3, "새 결과"));
            Search("이전");
            yield return WaitFor(() => CardCount == 2, "first result");
            Search("새 검색");
            yield return WaitFor(() => _repository.CallCount == 2 && CardCount == 1, "replacement result");
            Assert.That(_resultCount.text, Is.EqualTo("RESULTS 01"));
            Assert.That(_resultsGrid.Q<Label>("cardTitle").text, Is.EqualTo("새 결과"));
        }

        [UnityTest]
        public IEnumerator RapidSearchShowsOnlyLatestResultAfterLateResponse()
        {
            var first = _repository.EnqueuePending();
            _repository.EnqueueSuccess(Item(2, "마지막 결과"));
            Search("첫 검색");
            yield return WaitFor(() => _repository.CallCount == 1, "first request");
            Search("두 번째 검색");
            yield return WaitFor(() => _repository.CallCount == 2 && CardCount == 1, "second result");
            Assert.That(_repository.FirstToken.IsCancellationRequested, Is.True);
            first.TrySetResult(RecordingSearchRepository.Success(Item(1, "늦은 결과")));
            yield return null;
            Assert.That(CardCount, Is.EqualTo(1));
            Assert.That(_resultsGrid.Q<Label>("cardTitle").text, Is.EqualTo("마지막 결과"));
        }

        private IEnumerator ErrorShowsMessage(NetworkError error, string message)
        {
            _repository.EnqueueError(error);
            Search("오류 검색");
            yield return WaitFor(() => _repository.CallCount == 1 && _statusLabel.text == message, "error message");
            Assert.That(CardCount, Is.Zero);
            Assert.That(_resultCount.text, Is.EqualTo("RESULTS 00"));
        }

        private void Search(string keyword)
        {
            _searchField.value = keyword;
            _searchButton.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled()) _searchButton.SendEvent(submit);
        }

        private int CardCount => _resultsGrid.Query<VisualElement>(className: "result-card-instance").ToList().Count;

        private static IEnumerator WaitFor(Func<bool> condition, string description)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Timed out waiting for " + description);
        }

        private static ImageItem Item(long id, string title) => new ImageItem(id,
            new Uri("mock://thumb/" + id), 4.0 / 3.0,
            new Uri("https://example.invalid/image/" + id), new[] { title });

        private sealed class RecordingSearchRepository : IImageSearchRepository
        {
            private readonly Queue<UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>>> _responses =
                new Queue<UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>>>();
            public int CallCount { get; private set; }
            public CancellationToken FirstToken { get; private set; }

            public void EnqueueSuccess(params ImageItem[] items) => _responses.Enqueue(UniTask.FromResult(Success(items)));
            public void EnqueueError(NetworkError error) => _responses.Enqueue(UniTask.FromResult(
                Result<IReadOnlyList<ImageItem>, NetworkError>.FromError(error)));
            public UniTaskCompletionSource<Result<IReadOnlyList<ImageItem>, NetworkError>> EnqueuePending()
            {
                var pending = new UniTaskCompletionSource<Result<IReadOnlyList<ImageItem>, NetworkError>>();
                _responses.Enqueue(pending.Task);
                return pending;
            }
            public static Result<IReadOnlyList<ImageItem>, NetworkError> Success(params ImageItem[] items) =>
                Result<IReadOnlyList<ImageItem>, NetworkError>.FromSuccess(items);
            public UniTask<Result<IReadOnlyList<ImageItem>, NetworkError>> SearchAsync(
                string keyword, int page, int pageSize, CancellationToken cancellationToken)
            {
                if (CallCount++ == 0) FirstToken = cancellationToken;
                return _responses.Dequeue();
            }
        }

        private sealed class OfflineThumbnailRepository : IImageThumbnailRepository
        {
            public UniTask<Result<byte[], NetworkError>> LoadAsync(Uri thumbnailUrl, CancellationToken cancellationToken) =>
                UniTask.FromResult(Result<byte[], NetworkError>.FromError(NetworkError.Unknown));
        }
    }
}
#endif
