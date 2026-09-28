using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Core;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Data.Pixabay.Repositories;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayImageThumbnailRepositoryTests
    {
        private static readonly Uri Url = new Uri("https://example.invalid/thumb.jpg");

        [Test]
        public void SuccessReturnsDownloadedBytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var result = new PixabayImageThumbnailRepository(new FakeThumbnailDataSource(bytes))
                .LoadAsync(Url, CancellationToken.None).AsTask().GetAwaiter().GetResult();

            Assert.That(result, Is.TypeOf<Result<byte[], NetworkError>.Success>());
            Assert.That(((Result<byte[], NetworkError>.Success)result).Data, Is.SameAs(bytes));
        }

        [Test]
        public void ConnectionFailureReturnsNetworkError()
        {
            var result = new PixabayImageThumbnailRepository(new FakeThumbnailDataSource(
                    new PixabayNetworkException(PixabayTransportFailure.ConnectionFailure)))
                .LoadAsync(Url, CancellationToken.None).AsTask().GetAwaiter().GetResult();

            Assert.That(((Result<byte[], NetworkError>.Error)result).Value, Is.EqualTo(NetworkError.ConnectionFailure));
        }

        [Test]
        public void CancellationPropagates()
        {
            var repository = new PixabayImageThumbnailRepository(new FakeThumbnailDataSource(new OperationCanceledException()));
            Assert.Throws<OperationCanceledException>(() =>
                repository.LoadAsync(Url, CancellationToken.None).AsTask().GetAwaiter().GetResult());
        }

        private sealed class FakeThumbnailDataSource : IPixabayImageThumbnailDataSource
        {
            private readonly byte[] _bytes;
            private readonly Exception _exception;
            public FakeThumbnailDataSource(byte[] bytes) => _bytes = bytes;
            public FakeThumbnailDataSource(Exception exception) => _exception = exception;
            public UniTask<byte[]> LoadAsync(Uri url, CancellationToken cancellationToken)
            {
                if (_exception != null) throw _exception;
                return UniTask.FromResult(_bytes);
            }
        }
    }
}
