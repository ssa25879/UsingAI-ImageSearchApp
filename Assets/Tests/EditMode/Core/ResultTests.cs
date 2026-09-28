using ImageSearch.Core;
using NUnit.Framework;

namespace ImageSearch.Tests.EditMode.Core
{
    public sealed class ResultTests
    {
        [Test]
        public void Success_PreservesData_AndPatternMatches()
        {
            Result<string, NetworkError> result = Result<string, NetworkError>.FromSuccess("images");

            Assert.That(result, Is.TypeOf<Result<string, NetworkError>.Success>());
            Assert.That(((Result<string, NetworkError>.Success)result).Data, Is.EqualTo("images"));
        }

        [Test]
        public void Error_PreservesNetworkError_AndPatternMatches()
        {
            Result<string, NetworkError> result = Result<string, NetworkError>.FromError(NetworkError.NotFound);

            Assert.That(result, Is.TypeOf<Result<string, NetworkError>.Error>());
            Assert.That(((Result<string, NetworkError>.Error)result).Value, Is.EqualTo(NetworkError.NotFound));
        }

        [Test]
        public void Branches_AreSealed_AndPayloadHasNoPublicSetter()
        {
            Assert.That(typeof(Result<int, int>.Success).IsSealed, Is.True);
            Assert.That(typeof(Result<int, int>.Error).IsSealed, Is.True);
            Assert.That(typeof(Result<int, int>.Success).GetProperty("Data").SetMethod, Is.Null);
            Assert.That(typeof(Result<int, int>.Error).GetProperty("Value").SetMethod, Is.Null);
        }

        [Test]
        public void RecordEquality_UsesBranchAndPayload()
        {
            var first = Result<int, string>.FromSuccess(3);
            var second = Result<int, string>.FromSuccess(3);
            var error = Result<int, string>.FromError("failed");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.EqualTo(error));
        }
    }
}
