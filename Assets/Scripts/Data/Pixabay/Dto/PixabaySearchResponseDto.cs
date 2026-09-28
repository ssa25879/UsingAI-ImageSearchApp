using System;
using System.Collections.Generic;

namespace ImageSearch.Data.Pixabay.Dto
{
    [Serializable]
    public sealed class PixabaySearchResponseDto
    {
        public int total;
        public int totalHits;
        public List<PixabayHitDto> hits;
    }
}
