using System;
using System.IO;
using ImageSearch.Data.Pixabay.DataSource;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabaySearchCacheTests
    {
        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "pixabay-cache-" + Guid.NewGuid());

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void CachedResponseIsReusedForTwentyFourHours()
        {
            var cache = new PixabaySearchCache(_directory);
            var now = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);
            cache.Store("query-a", "{\"hits\":[]}", now);

            Assert.That(cache.TryRead("query-a", now.AddHours(23), out var response), Is.True);
            Assert.That(response, Is.EqualTo("{\"hits\":[]}"));
            Assert.That(cache.TryRead("query-a", now.AddHours(24), out _), Is.False);
        }

        [Test]
        public void DifferentQueryDoesNotReuseResponse()
        {
            var cache = new PixabaySearchCache(_directory);
            var now = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);
            cache.Store("query-a", "response", now);

            Assert.That(cache.TryRead("query-b", now, out _), Is.False);
        }

        [Test]
        public void CacheFileNameDoesNotContainQueryOrKey()
        {
            var cache = new PixabaySearchCache(_directory);
            cache.Store("secret-key?query=cat", "response", DateTime.UtcNow);

            var fileName = Path.GetFileName(Directory.GetFiles(_directory)[0]);
            Assert.That(fileName, Does.Not.Contain("secret-key"));
            Assert.That(fileName, Does.Not.Contain("cat"));
        }
    }
}
