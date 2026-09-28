using System.IO;
using ImageSearch.Data.Pixabay.Dto;
using NUnit.Framework;
using UnityEngine;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayDtoTests
    {
        [Test]
        public void JsonUtility_DeserializesPixabayCaseSensitiveFields()
        {
            var json = File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests/EditMode/Data/Fixtures/pixabay-success.json"));
            var response = JsonUtility.FromJson<PixabaySearchResponseDto>(json);

            Assert.That(response.totalHits, Is.EqualTo(1));
            Assert.That(response.hits, Has.Count.EqualTo(1));
            Assert.That(response.hits[0].webformatURL, Is.EqualTo("https://cdn.example.test/image.jpg"));
            Assert.That(response.hits[0].user_id, Is.EqualTo(7));
            Assert.That(response.hits[0].fullHDURL, Is.Null);
        }

        [Test]
        public void JsonUtility_DeserializesEmptyHitList()
        {
            var json = File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests/EditMode/Data/Fixtures/pixabay-empty.json"));
            var response = JsonUtility.FromJson<PixabaySearchResponseDto>(json);

            Assert.That(response.hits, Is.Empty);
        }
    }
}
