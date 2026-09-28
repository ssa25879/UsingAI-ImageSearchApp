using System;
using System.Collections.Generic;
using ImageSearch.Domain.Models;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Domain
{
    public sealed class ImageItemTests
    {
        [Test]
        public void ImageItem_StoresScreenFields()
        {
            var thumbnail = new Uri("https://cdn.example.test/thumb.jpg");
            var source = new Uri("https://pixabay.com/photos/sample-1/");
            var item = new ImageItem(1, thumbnail, 1.5, source, new[] { "cat", "pet" });

            Assert.That(item.Id, Is.EqualTo(1));
            Assert.That(item.ThumbnailUrl, Is.EqualTo(thumbnail));
            Assert.That(item.AspectRatio, Is.EqualTo(1.5));
            Assert.That(item.SourcePageUrl, Is.EqualTo(source));
            Assert.That(item.Tags, Is.EqualTo(new[] { "cat", "pet" }));
            Assert.That(item.GetType().GetProperty("Id").SetMethod, Is.Null);
        }

        [Test]
        public void ImageItem_CopiesTagsIntoReadOnlySnapshot()
        {
            var sourceTags = new List<string> { "first" };
            var item = new ImageItem(1, new Uri("https://cdn.example.test/a.jpg"), 1,
                new Uri("https://pixabay.com/a/"), sourceTags);

            sourceTags.Add("later");

            Assert.That(item.Tags, Is.EqualTo(new[] { "first" }));
            Assert.That(item.Tags, Is.InstanceOf<IReadOnlyList<string>>());
        }
    }
}
