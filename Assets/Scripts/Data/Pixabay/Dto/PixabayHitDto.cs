using System;

namespace ImageSearch.Data.Pixabay.Dto
{
    [Serializable]
    public sealed class PixabayHitDto
    {
        public long id;
        public string pageURL;
        public string type;
        public string tags;
        public string previewURL;
        public int previewWidth;
        public int previewHeight;
        public string webformatURL;
        public int webformatWidth;
        public int webformatHeight;
        public string largeImageURL;
        public string fullHDURL;
        public string imageURL;
        public int imageWidth;
        public int imageHeight;
        public long imageSize;
        public int views;
        public int downloads;
        public int likes;
        public int comments;
        public long user_id;
        public string user;
        public string userImageURL;
    }
}
