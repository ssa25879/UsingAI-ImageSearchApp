using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ImageSearch.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SearchScreenPreviewController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _resultCardTemplate;

        private static readonly PreviewImage[] PreviewImages =
        {
            new PreviewImage("초록빛 산책", "숲 · 자연", "forest", "PIX / 042"),
            new PreviewImage("파란 해변", "바다 · 여행", "coast", "PIX / 118"),
            new PreviewImage("봄날의 꽃", "꽃 · 정원", "bloom", "PIX / 207"),
            new PreviewImage("따뜻한 오후", "풍경 · 햇살", "desert", "PIX / 316"),
            new PreviewImage("고요한 밤", "하늘 · 별빛", "night", "PIX / 429"),
            new PreviewImage("작은 카페", "카페 · 일상", "cafe", "PIX / 503")
        };

        private UIDocument _document;
        private VisualElement _safeArea;
        private VisualElement _resultsGrid;
        private TextField _searchField;
        private Label _searchPlaceholder;
        private Label _statusLabel;
        private Label _resultCount;
        private Button _searchButton;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _searchFieldFocused;

        private void Start()
        {
            _document = GetComponent<UIDocument>();
            var root = _document.rootVisualElement;
            _safeArea = root.Q<VisualElement>("safeArea");
            _resultsGrid = root.Q<VisualElement>("resultsGrid");
            _searchField = root.Q<TextField>("searchField");
            _searchPlaceholder = root.Q<Label>("searchPlaceholder");
            _statusLabel = root.Q<Label>("statusLabel");
            _resultCount = root.Q<Label>("resultCount");
            _searchButton = root.Q<Button>("searchButton");

            if (_resultCardTemplate == null || _resultsGrid == null || _searchField == null || _searchPlaceholder == null ||
                _statusLabel == null || _resultCount == null || _searchButton == null || _safeArea == null)
            {
                Debug.LogError("Search screen UXML is missing a required named element or card template.", this);
                enabled = false;
                return;
            }

            _searchButton.clicked += SearchPreviewData;
            _searchField.RegisterCallback<FocusInEvent>(OnSearchFocusIn);
            _searchField.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            RefreshSearchPlaceholder();
            ApplySafeArea();
            ShowPreview(PreviewImages);
            _statusLabel.text = "샘플 이미지를 둘러보세요. 실제 검색은 아직 연결되지 않았어요.";
        }

        private void OnDestroy()
        {
            if (_searchButton != null)
                _searchButton.clicked -= SearchPreviewData;
            if (_searchField != null)
            {
                _searchField.UnregisterCallback<FocusInEvent>(OnSearchFocusIn);
                _searchField.UnregisterCallback<FocusOutEvent>(OnSearchFocusOut);
                _searchField.UnregisterValueChangedCallback(OnSearchValueChanged);
            }
        }

        private void OnSearchFocusIn(FocusInEvent evt)
        {
            _searchFieldFocused = true;
            RefreshSearchPlaceholder();
        }

        private void OnSearchFocusOut(FocusOutEvent evt)
        {
            _searchFieldFocused = false;
            RefreshSearchPlaceholder();
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt) => RefreshSearchPlaceholder();

        private void RefreshSearchPlaceholder()
        {
            if (_searchPlaceholder != null && _searchField != null)
            {
                _searchPlaceholder.style.display = string.IsNullOrEmpty(_searchField.value) && !_searchFieldFocused
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight ||
                Screen.safeArea != _lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void SearchPreviewData()
        {
            var keyword = _searchField.value?.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                _statusLabel.text = "검색어를 입력해 주세요. 이 화면은 임시 데이터로만 동작합니다.";
                ShowPreview(PreviewImages);
                return;
            }

            var matches = new List<PreviewImage>();
            foreach (var image in PreviewImages)
            {
                if (image.Title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    image.Tags.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(image);
                }
            }

            ShowPreview(matches);
            _statusLabel.text = matches.Count == 0
                ? "임시 데이터에서 일치하는 이미지가 없어요. 실제 검색은 연결 전입니다."
                : $"임시 데이터 {matches.Count}개 · 실제 검색 연결 전";
        }

        private void ShowPreview(IReadOnlyList<PreviewImage> images)
        {
            _resultsGrid.Clear();
            _resultCount.text = $"SAMPLE {images.Count:00}";

            for (var index = 0; index < images.Count; index += 2)
            {
                var row = new VisualElement();
                row.AddToClassList("result-row");
                row.Add(MakeCard(images[index], false));

                if (index + 1 < images.Count)
                {
                    row.Add(MakeCard(images[index + 1], true));
                }
                else
                {
                    var spacer = new VisualElement();
                    spacer.AddToClassList("result-card-spacer");
                    row.Add(spacer);
                }

                _resultsGrid.Add(row);
            }
        }

        private VisualElement MakeCard(PreviewImage image, bool lastInRow)
        {
            var card = _resultCardTemplate.CloneTree();
            card.AddToClassList("result-card-instance");
            card.Q<Label>("cardTitle").text = image.Title;
            card.Q<Label>("cardTags").text = image.Tags;

            var artwork = card.Q<VisualElement>("artwork");
            artwork.AddToClassList("art-" + image.Artwork);
            artwork.Q<Label>(className: "art-label").text = image.Code;
            if (lastInRow)
                card.AddToClassList("result-card-instance-last");

            return card;
        }

        private void ApplySafeArea()
        {
            if (_safeArea == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            var area = Screen.safeArea;
            var left = area.xMin / Screen.width * 100f;
            var right = (Screen.width - area.xMax) / Screen.width * 100f;
            var top = (Screen.height - area.yMax) / Screen.height * 100f;
            var bottom = area.yMin / Screen.height * 100f;

            _safeArea.style.left = Length.Percent(left);
            _safeArea.style.right = Length.Percent(right);
            _safeArea.style.top = Length.Percent(top);
            _safeArea.style.bottom = Length.Percent(bottom);

            _lastSafeArea = area;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private sealed class PreviewImage
        {
            public string Title { get; }
            public string Tags { get; }
            public string Artwork { get; }
            public string Code { get; }

            public PreviewImage(string title, string tags, string artwork, string code)
            {
                Title = title;
                Tags = tags;
                Artwork = artwork;
                Code = code;
            }
        }
    }
}
