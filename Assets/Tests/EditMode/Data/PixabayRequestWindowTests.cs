using System;
using ImageSearch.Data.Pixabay.DataSource;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class PixabayRequestWindowTests
    {
        [Test]
        public void AllowsAtMostOneHundredRequestsPerMinute()
        {
            var window = new PixabayRequestWindow();
            var now = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 100; i++) Assert.That(window.TryEnter(now), Is.True);
            Assert.That(window.TryEnter(now), Is.False);
            Assert.That(window.TryEnter(now.AddSeconds(60)), Is.True);
        }
    }
}
