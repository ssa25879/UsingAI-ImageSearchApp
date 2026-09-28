using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ImageSearch.Domain.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace ImageSearch.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SearchScreenPreviewController : MonoBehaviour, ISearchScreenView
    {
        [SerializeField] private VisualTreeAsset _resultCardTemplate;
        private UIDocument _document;
        private VisualElement _safeArea, _resultsGrid;
        private TextField _searchField;
        private Label _searchPlaceholder, _statusLabel, _resultCount, _attributionLabel;
        private Button _searchButton;
        private SearchScreenPresenter _presenter;
        private readonly Dictionary<long, Texture2D> _ownedTextures = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, VisualElement> _artworks = new Dictionary<long, VisualElement>();
        private Rect _lastSafeArea;
        private int _lastScreenWidth, _lastScreenHeight;
        private bool _searchFieldFocused;
        private bool _isPixabaySearch;

        public void Initialize(SearchScreenPresenter presenter) => _presenter = presenter;
        public void SetPixabayAttribution(bool isPixabaySearch)
        {
            _isPixabaySearch = isPixabaySearch;
            if (_attributionLabel != null) _attributionLabel.text = isPixabaySearch ? "이미지 제공: Pixabay" : "미리보기용 이미지";
        }

        private void Start()
        {
            _document = GetComponent<UIDocument>(); var root = _document.rootVisualElement;
            _safeArea = root.Q<VisualElement>("safeArea"); _resultsGrid = root.Q<VisualElement>("resultsGrid");
            _searchField = root.Q<TextField>("searchField"); _searchPlaceholder = root.Q<Label>("searchPlaceholder");
            _statusLabel = root.Q<Label>("statusLabel"); _resultCount = root.Q<Label>("resultCount");
            _attributionLabel = root.Q<Label>("attributionLabel");
            _searchButton = root.Q<Button>("searchButton");
            if (_resultCardTemplate == null || _resultsGrid == null || _searchField == null || _searchPlaceholder == null || _statusLabel == null || _resultCount == null || _attributionLabel == null || _searchButton == null || _safeArea == null)
            { Debug.LogError("Search screen UXML is missing a required element or card template.", this); enabled = false; return; }
            _searchButton.clicked += OnSearchClicked;
            _searchField.RegisterCallback<FocusInEvent>(OnSearchFocusIn); _searchField.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            SetPixabayAttribution(_isPixabaySearch);
            RefreshSearchPlaceholder(); ApplySafeArea(); ShowEmptyResults();
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            ReleaseTextures();
            if (_searchButton != null) _searchButton.clicked -= OnSearchClicked;
            if (_searchField != null) { _searchField.UnregisterCallback<FocusInEvent>(OnSearchFocusIn); _searchField.UnregisterCallback<FocusOutEvent>(OnSearchFocusOut); _searchField.UnregisterValueChangedCallback(OnSearchValueChanged); }
        }
        private void OnSearchClicked()
        {
            var keyword = _searchField.value?.Trim();
            if (_presenter != null && !string.IsNullOrEmpty(keyword)) _presenter.SearchAsync(keyword).Forget();
        }
        private void OnSearchFocusIn(FocusInEvent evt) { _searchFieldFocused = true; RefreshSearchPlaceholder(); }
        private void OnSearchFocusOut(FocusOutEvent evt) { _searchFieldFocused = false; RefreshSearchPlaceholder(); }
        private void OnSearchValueChanged(ChangeEvent<string> evt) => RefreshSearchPlaceholder();
        private void RefreshSearchPlaceholder() { if (_searchPlaceholder != null && _searchField != null) _searchPlaceholder.style.display = string.IsNullOrEmpty(_searchField.value) && !_searchFieldFocused ? DisplayStyle.Flex : DisplayStyle.None; }
        private void Update() { if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight || Screen.safeArea != _lastSafeArea) ApplySafeArea(); }

        public void ShowSearching() { _statusLabel.text = "검색 중이에요..."; }
        public void ShowEmptyResults() { ClearResults(); _resultCount.text = "RESULTS 00"; _statusLabel.text = "검색 결과가 없어요. 다른 키워드로 찾아보세요."; _searchButton?.SetEnabled(true); }
        public void ShowError(string message) { ClearResults(); _resultCount.text = "RESULTS 00"; _statusLabel.text = message; _searchButton?.SetEnabled(true); }
        public void ShowResults(IReadOnlyList<ImageItem> items)
        {
            ClearResults(); _resultCount.text = $"RESULTS {items.Count:00}"; _statusLabel.text = $"이미지 {items.Count}개를 찾았어요.";
            for (var i = 0; i < items.Count; i += 2)
            {
                var row = new VisualElement(); row.AddToClassList("result-row"); row.Add(MakeCard(items[i], i));
                if (i + 1 < items.Count) row.Add(MakeCard(items[i + 1], i + 1)); else { var spacer = new VisualElement(); spacer.AddToClassList("result-card-spacer"); row.Add(spacer); }
                _resultsGrid.Add(row);
            }
            _searchButton.SetEnabled(true);
        }
        private VisualElement MakeCard(ImageItem image, int index)
        {
            var card = _resultCardTemplate.CloneTree(); card.AddToClassList("result-card-instance");
            card.Q<Label>("cardTitle").text = image.Tags.Count > 0 ? image.Tags[0] : "이미지 " + image.Id;
            card.Q<Label>("cardTags").text = string.Join(" · ", image.Tags);
            var artwork = card.Q<VisualElement>("artwork");
            _artworks[image.Id] = artwork;
            var styles = new[] { "forest", "coast", "bloom", "desert", "night", "cafe" };
            artwork.AddToClassList("art-" + styles[index % styles.Length]); artwork.Q<Label>(className: "art-label").text = $"IMG / {image.Id:000}";
            if ((index & 1) == 1) card.AddToClassList("result-card-instance-last");
            return card;
        }
        public void ClearResults() { ReleaseTextures(); _artworks.Clear(); _resultsGrid?.Clear(); }
        public void SetThumbnail(long imageId, Texture2D texture)
        {
            if (!_artworks.TryGetValue(imageId, out var artwork)) { Destroy(texture); return; }
            if (_ownedTextures.TryGetValue(imageId, out var previous)) Destroy(previous);
            _ownedTextures[imageId] = texture;
            artwork.style.backgroundImage = new StyleBackground(texture);
            foreach (var child in artwork.Children()) child.style.display = DisplayStyle.None;
        }
        public void ShowThumbnailPlaceholder(long imageId) { }
        private void ReleaseTextures()
        {
            foreach (var texture in _ownedTextures.Values) if (texture != null) Destroy(texture);
            _ownedTextures.Clear();
        }
        private void ApplySafeArea()
        {
            if (_safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea; _safeArea.style.left = Length.Percent(area.xMin / Screen.width * 100f);
            _safeArea.style.right = Length.Percent((Screen.width - area.xMax) / Screen.width * 100f);
            _safeArea.style.top = Length.Percent((Screen.height - area.yMax) / Screen.height * 100f);
            _safeArea.style.bottom = Length.Percent(area.yMin / Screen.height * 100f);
            _lastSafeArea = area; _lastScreenWidth = Screen.width; _lastScreenHeight = Screen.height;
        }
    }
}
