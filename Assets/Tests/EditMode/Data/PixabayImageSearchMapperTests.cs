using System;
using ImageSearch.Data.Pixabay.Dto;
using ImageSearch.Data.Pixabay.Exceptions;
using ImageSearch.Data.Pixabay.Mapper;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayImageSearchMapperTests
    {
        private static PixabayHitDto ValidHit(string tags = "Fox, red animal, , wildlife") => new PixabayHitDto
        {
            id = 42,
            pageURL = "https://pixabay.com/photos/red-fox-42/",
            tags = tags,
            webformatURL = "https://cdn.example.test/image.jpg",
            webformatWidth = 640,
            webformatHeight = 480
        };

        [Test]
        public void Mapper_MapsHitToImageItem()
        {
            var item = new PixabayImageSearchMapper().ToModel(ValidHit());

            Assert.That(item.Id, Is.EqualTo(42));
            Assert.That(item.ThumbnailUrl.AbsoluteUri, Is.EqualTo("https://cdn.example.test/image.jpg"));
            Assert.That(item.AspectRatio, Is.EqualTo(4.0 / 3.0).Within(0.0001));
            Assert.That(item.SourcePageUrl.AbsoluteUri, Is.EqualTo("https://pixabay.com/photos/red-fox-42/"));
            Assert.That(item.Tags, Is.EqualTo(new[] { "Fox", "red animal", "wildlife" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" , , ")]
        public void ParseTags_NullOrEmptyTokens_ReturnsEmptyList(string tags)
        {
            Assert.That(new PixabayImageSearchMapper().ParseTags(tags), Is.Empty);
        }

        [Test]
        public void ParseTags_SplitsTrimsDropsEmptyTokens_AndPreservesOrderAndCase()
        {
            Assert.That(new PixabayImageSearchMapper().ParseTags(" A, b,,Fox "),
                Is.EqualTo(new[] { "A", "b", "Fox" }));
        }

        [Test]
        public void Mapper_InvalidRequiredUrl_ThrowsResponseException()
        {
            var hit = ValidHit();
            hit.webformatURL = "not a URL";

            Assert.Throws<PixabayResponseException>(() => new PixabayImageSearchMapper().ToModel(hit));
        }
    }
}
