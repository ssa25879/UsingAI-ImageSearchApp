using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Data.Pixabay.Mapper;
using ImageSearch.Data.Pixabay.Repositories;
using ImageSearch.Domain.Models;
using ImageSearch.Tests.EditMode.Data.Fakes;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayImageSearchRepositoryTests
    {
        private static PixabaySearchResponseDto OneHit() => new PixabaySearchResponseDto
        {
            total = 1,
            totalHits = 1,
            hits = new List<PixabayHitDto>
            {
                new PixabayHitDto
                {
                    id = 42,
                    pageURL = "https://pixabay.com/photos/fox-42/",
                    tags = "Fox, wildlife",
                    webformatURL = "https://cdn.example.test/fox.jpg",
                    webformatWidth = 640,
                    webformatHeight = 480
                }
            }
        };

        private static Result<IReadOnlyList<ImageItem>, NetworkError> Search(
            PixabayImageSearchRepository repository, string keyword = "fox", CancellationToken token = default)
            => repository.SearchAsync(keyword, 2, 25, token).AsTask().GetAwaiter().GetResult();

        [Test]
        public void Repository_SearchSuccess_MapsHitsAndPassesPaging()
        {
            var fake = new FakePixabayImageSearchDataSource { Response = OneHit() };
            var repository = new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper());

            var result = Search(repository);

            Assert.That(result, Is.TypeOf<Result<IReadOnlyList<ImageItem>, NetworkError>.Success>());
            var items = ((Result<IReadOnlyList<ImageItem>, NetworkError>.Success)result).Data;
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].Id, Is.EqualTo(42));
            Assert.That(fake.CallCount, Is.EqualTo(1));
            Assert.That(fake.LastKeyword, Is.EqualTo("fox"));
            Assert.That(fake.LastPage, Is.EqualTo(2));
            Assert.That(fake.LastPageSize, Is.EqualTo(25));
        }

        [Test]
        public void Repository_ZeroHits_ReturnsSuccessWithEmptyList()
        {
            var fake = new FakePixabayImageSearchDataSource
            {
                Response = new PixabaySearchResponseDto { total = 0, totalHits = 0, hits = new List<PixabayHitDto>() }
            };
            var result = Search(new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper()));

            Assert.That(result, Is.TypeOf<Result<IReadOnlyList<ImageItem>, NetworkError>.Success>());
            Assert.That(((Result<IReadOnlyList<ImageItem>, NetworkError>.Success)result).Data, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Repository_BlankOrWhitespaceKeyword_DoesNotCallDataSource(string keyword)
        {
            var fake = new FakePixabayImageSearchDataSource();
            var result = Search(new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper()), keyword);

            Assert.That(fake.CallCount, Is.Zero);
            Assert.That(result, Is.TypeOf<Result<IReadOnlyList<ImageItem>, NetworkError>.Success>());
            Assert.That(((Result<IReadOnlyList<ImageItem>, NetworkError>.Success)result).Data, Is.Empty);
        }

        [TestCase(PixabayTransportFailure.NoInternet, NetworkError.NoInternet)]
        [TestCase(PixabayTransportFailure.Timeout, NetworkError.NetworkTimeout)]
        [TestCase(PixabayTransportFailure.ConnectionFailure, NetworkError.ConnectionFailure)]
        public void Repository_MapsTransportFailures(PixabayTransportFailure failure, NetworkError expected)
        {
            var fake = new FakePixabayImageSearchDataSource { ExceptionToThrow = new PixabayNetworkException(failure) };
            var result = Search(new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper()));

            AssertError(result, expected);
        }

        [TestCase(400, NetworkError.BadRequest)]
        [TestCase(401, NetworkError.Unauthorized)]
        [TestCase(403, NetworkError.Forbidden)]
        [TestCase(404, NetworkError.NotFound)]
        [TestCase(429, NetworkError.RateLimited)]
        [TestCase(500, NetworkError.ServerError)]
        [TestCase(503, NetworkError.ServerError)]
        public void Repository_MapsHttpStatusCodes(int statusCode, NetworkError expected)
        {
            var fake = new FakePixabayImageSearchDataSource
            {
                ExceptionToThrow = new PixabayHttpException(statusCode, "response")
            };
            var result = Search(new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper()));

            AssertError(result, expected);
        }

        [Test]
        public void Repository_ResponseFailureMapsToUnknown()
        {
            var fake = new FakePixabayImageSearchDataSource
            {
                ExceptionToThrow = new PixabayResponseException("invalid response")
            };
            AssertError(Search(new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper())), NetworkError.Unknown);
        }

        [Test]
        public void Repository_Cancellation_RethrowsOperationCanceledException()
        {
            var fake = new FakePixabayImageSearchDataSource
            {
                ExceptionToThrow = new OperationCanceledException()
            };
            var repository = new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper());

            Assert.Throws<OperationCanceledException>(() => Search(repository));
        }

        [Test]
        public void Repository_ProgrammingError_IsNotConvertedToUnknown()
        {
            var fake = new FakePixabayImageSearchDataSource { ExceptionToThrow = new InvalidOperationException("bug") };
            var repository = new PixabayImageSearchRepository(fake, new PixabayImageSearchMapper());

            Assert.Throws<InvalidOperationException>(() => Search(repository));
        }

        private static void AssertError(Result<IReadOnlyList<ImageItem>, NetworkError> result, NetworkError expected)
        {
            Assert.That(result, Is.TypeOf<Result<IReadOnlyList<ImageItem>, NetworkError>.Error>());
            Assert.That(((Result<IReadOnlyList<ImageItem>, NetworkError>.Error)result).Value, Is.EqualTo(expected));
        }
    }
}
