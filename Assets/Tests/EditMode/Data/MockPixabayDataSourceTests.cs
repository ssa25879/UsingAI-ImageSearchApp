using System.Threading;
using Cysharp.Threading.Tasks;
using ImageSearch.Data.Pixabay.DataSource;
using ImageSearch.Data.Pixabay.Exceptions;
using NUnit.Framework;
using UnityEngine;

namespace ImageSearch.Tests.EditMode.Data
{
    public sealed class MockPixabayDataSourceTests
    {
        private GameObject _host;
        private MockPixabayImageSearchDataSource _source;

        [SetUp] public void SetUp() { _host = new GameObject("mock-source-test"); _source = _host.AddComponent<MockPixabayImageSearchDataSource>(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_host); }

        [Test] public async System.Threading.Tasks.Task SuccessReturnsFixtureHits()
        {
            var result = await _source.SearchAsync("forest", 1, 4, CancellationToken.None);
            Assert.That(result.hits, Has.Count.EqualTo(4));
            Assert.That(result.hits[0].tags, Does.Contain("forest"));
        }

        [Test] public async System.Threading.Tasks.Task ScenarioChangeAppliesToNextSearch()
        {
            _source.Scenario = MockSearchScenario.EmptyResults;
            var result = await _source.SearchAsync("forest", 1, 4, CancellationToken.None);
            Assert.That(result.hits, Is.Empty);
        }

        [Test] public void ConnectionScenarioThrowsExpectedFailure()
        {
            _source.Scenario = MockSearchScenario.ConnectionFailure;
            Assert.ThrowsAsync<PixabayNetworkException>(async () => await _source.SearchAsync("x", 1, 4, CancellationToken.None));
        }

        [Test] public void ServerScenarioThrowsExpectedFailure()
        {
            _source.Scenario = MockSearchScenario.ServerError;
            Assert.ThrowsAsync<PixabayHttpException>(async () => await _source.SearchAsync("x", 1, 4, CancellationToken.None));
        }
    }
}
