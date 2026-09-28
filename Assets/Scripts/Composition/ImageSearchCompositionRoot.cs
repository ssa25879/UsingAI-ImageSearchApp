using System.IO;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Mapper;
using ImageSearch.Data.Pixabay.Repositories;
using ImageSearch.Domain.Repositories;
using ImageSearch.UI;
using UnityEngine;

namespace ImageSearch.Composition
{
    public sealed class ImageSearchCompositionRoot : MonoBehaviour
    {
        [SerializeField] private SearchScreenPreviewController _screen;
        [SerializeField] private MockPixabayImageSearchDataSource _mockDataSource;
        [SerializeField] private bool _forceMock;

        private void Awake()
        {
            if (_screen == null || _mockDataSource == null)
            { Debug.LogError("Composition Root needs the screen and mock data source references.", this); enabled = false; return; }
            IPixabayImageSearchDataSource dataSource = _mockDataSource;
            IImageThumbnailRepository thumbnails = new MockImageThumbnailRepository();
            var usePixabay = false;
#if UNITY_EDITOR
            if (!_forceMock)
            {
                var settingsPath = Path.Combine(Application.dataPath, "..", "UserSettings", "ImageSearch");
                if (PixabayApiKeyLoader.TryRead(Path.Combine(settingsPath, "pixabay.key"), out var key))
                {
                    dataSource = new PixabayImageSearchDataSource(key, Path.Combine(settingsPath, "Cache"));
                    thumbnails = new PixabayImageThumbnailRepository(new PixabayImageThumbnailDataSource());
                    usePixabay = true;
                }
                else
                    Debug.LogWarning("Pixabay API key file is missing or empty. Using mock search data.", this);
            }
#else
            Debug.LogWarning("Local Pixabay API keys are Editor-only. Using mock search data.", this);
#endif
            var repository = new PixabayImageSearchRepository(dataSource, new PixabayImageSearchMapper());
            _screen.SetPixabayAttribution(usePixabay);
            _screen.Initialize(new SearchScreenPresenter(repository, thumbnails, _screen));
        }
    }
}
