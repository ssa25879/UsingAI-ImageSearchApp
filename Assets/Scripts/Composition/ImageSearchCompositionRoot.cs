using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Mapper;
using ImageSearch.Data.Pixabay.Repositories;
using ImageSearch.UI;
using UnityEngine;

namespace ImageSearch.Composition
{
    public sealed class ImageSearchCompositionRoot : MonoBehaviour
    {
        [SerializeField] private SearchScreenPreviewController _screen;
        [SerializeField] private MockPixabayImageSearchDataSource _mockDataSource;

        private void Awake()
        {
            if (_screen == null || _mockDataSource == null)
            { Debug.LogError("Composition Root needs the screen and mock data source references.", this); enabled = false; return; }
            var repository = new PixabayImageSearchRepository(_mockDataSource, new PixabayImageSearchMapper());
            var thumbnails = new MockImageThumbnailRepository();
            _screen.Initialize(new SearchScreenPresenter(repository, thumbnails, _screen));
        }
    }
}
