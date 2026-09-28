using System;
using System.Collections.Generic;
using System.Linq;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Domain.Models;

namespace ImageSearch.Data.Pixabay.Mapper
{
    public sealed class PixabayImageSearchMapper
    {
        public ImageItem ToModel(PixabayHitDto dto)
        {
            if (dto == null)
                throw new PixabayResponseException("Pixabay response contained a null hit.");

            if (!Uri.TryCreate(dto.webformatURL, UriKind.Absolute, out var thumbnailUrl) ||
                !Uri.TryCreate(dto.pageURL, UriKind.Absolute, out var sourcePageUrl) ||
                dto.webformatWidth <= 0 || dto.webformatHeight <= 0)
            {
                throw new PixabayResponseException("Pixabay hit is missing a valid URL or image dimensions.");
            }

            return new ImageItem(dto.id, thumbnailUrl,
                (double)dto.webformatWidth / dto.webformatHeight, sourcePageUrl, ParseTags(dto.tags));
        }

        public IReadOnlyList<string> ParseTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return Array.Empty<string>();

            return Array.AsReadOnly(tags.Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => tag.Length > 0)
                .ToArray());
        }
    }
}
