using System.IO;
using ImageSearch.Data.Pixabay.DataSource;
using NUnit.Framework;
using UnityEngine;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayDataSourceTests
    {
        [Test]
        public void BuildRequestUrl_EscapesKeyword_AndIncludesPaging()
        {
            var url = PixabayImageSearchDataSource.BuildRequestUrl("test-key", "red fox&blue", 2, 30);

            Assert.That(url, Does.StartWith("https://pixabay.com/api/?"));
            Assert.That(url, Does.Contain("key=test-key"));
            Assert.That(url, Does.Contain("q=red%20fox%26blue"));
            Assert.That(url, Does.Contain("page=2"));
            Assert.That(url, Does.Contain("per_page=30"));
            Assert.That(url, Does.Contain("image_type=photo"));
        }

        [Test]
        public void ParseResponse_DeserializesFixtureWithoutNetwork()
        {
            var json = File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests/EditMode/Data/Fixtures/pixabay-success.json"));
            var response = PixabayImageSearchDataSource.ParseResponse(json);

            Assert.That(response.hits, Has.Count.EqualTo(1));
            Assert.That(response.hits[0].id, Is.EqualTo(42));
        }
    }
}
