using System;
using System.IO;
using ImageSearch.Composition;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Composition
{
    public sealed class PixabayApiKeyLoaderTests
    {
        private string _keyFilePath;

        [SetUp]
        public void SetUp() => _keyFilePath = Path.Combine(Path.GetTempPath(), "image-search-" + Guid.NewGuid() + ".key");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_keyFilePath)) File.Delete(_keyFilePath);
        }

        [Test]
        public void MissingFileReturnsNoKey()
        {
            Assert.That(PixabayApiKeyLoader.TryRead(_keyFilePath, out var key), Is.False);
            Assert.That(key, Is.Null);
        }

        [Test]
        public void ExistingFileReturnsTrimmedKey()
        {
            File.WriteAllText(_keyFilePath, "  local-test-key  \r\n");

            Assert.That(PixabayApiKeyLoader.TryRead(_keyFilePath, out var key), Is.True);
            Assert.That(key, Is.EqualTo("local-test-key"));
        }

        [Test]
        public void EmptyFileReturnsNoKey()
        {
            File.WriteAllText(_keyFilePath, "  \r\n");

            Assert.That(PixabayApiKeyLoader.TryRead(_keyFilePath, out var key), Is.False);
            Assert.That(key, Is.Null);
        }
    }
}
