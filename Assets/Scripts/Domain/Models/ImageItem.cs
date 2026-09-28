using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageSearch.Domain.Models
{
    public sealed class ImageItem
    {
        private readonly IReadOnlyList<string> _tags;

        public long Id { get; }
        public Uri ThumbnailUrl { get; }
        public double AspectRatio { get; }
        public Uri SourcePageUrl { get; }
        public IReadOnlyList<string> Tags => _tags;

        public ImageItem(long id, Uri thumbnailUrl, double aspectRatio, Uri sourcePageUrl, IReadOnlyList<string> tags)
        {
            Id = id;
            ThumbnailUrl = thumbnailUrl ?? throw new ArgumentNullException(nameof(thumbnailUrl));
            if (aspectRatio <= 0 || double.IsNaN(aspectRatio) || double.IsInfinity(aspectRatio))
                throw new ArgumentOutOfRangeException(nameof(aspectRatio));
            AspectRatio = aspectRatio;
            SourcePageUrl = sourcePageUrl ?? throw new ArgumentNullException(nameof(sourcePageUrl));
            _tags = Array.AsReadOnly((tags ?? Array.Empty<string>()).ToArray());
        }
    }
}
