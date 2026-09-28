using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using UnityEngine;

namespace ImageSearch.Data.Pixabay.DataSource
{
    public sealed class MockPixabayImageSearchDataSource : MonoBehaviour, IPixabayImageSearchDataSource
    {
        [SerializeField] private MockSearchScenario _scenario = MockSearchScenario.Success;
        public MockSearchScenario Scenario { get => _scenario; set => _scenario = value; }

        public UniTask<PixabaySearchResponseDto> SearchAsync(string keyword, int page, int pageSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (_scenario)
            {
                case MockSearchScenario.EmptyResults:
                    return UniTask.FromResult(new PixabaySearchResponseDto { total = 0, totalHits = 0, hits = new List<PixabayHitDto>() });
                case MockSearchScenario.ConnectionFailure:
                    throw new PixabayNetworkException(PixabayTransportFailure.ConnectionFailure);
                case MockSearchScenario.ServerError:
                    throw new PixabayHttpException(500, "Mock server error");
                default:
                    return UniTask.FromResult(CreateSuccess(keyword, page, pageSize));
            }
        }

        private static PixabaySearchResponseDto CreateSuccess(string keyword, int page, int pageSize)
        {
            var titles = new[] { "초록빛 산책", "푸른 해안", "봄날의 정원", "따스한 오후", "고요한 밤", "작은 카페" };
            var hits = new List<PixabayHitDto>();
            var count = Math.Min(Math.Max(pageSize, 0), titles.Length);
            for (var i = 0; i < count; i++)
            {
                var id = (page - 1) * pageSize + i + 1;
                hits.Add(new PixabayHitDto {
                    id = id, pageURL = "https://example.invalid/image/" + id,
                    webformatURL = "mock://thumb/" + id, webformatWidth = 640, webformatHeight = 480,
                    tags = keyword + ", " + titles[i]
                });
            }
            return new PixabaySearchResponseDto { total = hits.Count, totalHits = hits.Count, hits = hits };
        }
    }
}
