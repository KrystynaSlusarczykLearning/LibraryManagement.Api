using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class PipelineTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(PipelineTests);
    private const string ResponseTimeHeader = "X-Response-Time-Ms";

    [TestCase("/api/categories", HttpStatusCode.OK)]
    [TestCase("/api/books", HttpStatusCode.OK)]
    [TestCase("/api/books/99", HttpStatusCode.NotFound)]
    public async Task AnyResponse_ShouldContainResponseTimeHeader(
        string uri, HttpStatusCode expectedStatusCode)
    {
        var response = await Client.GetAsync(uri);

        Assert.That(response.StatusCode, Is.EqualTo(expectedStatusCode));
        Assert.That(
            int.TryParse(response.GetHeaderValue(ResponseTimeHeader), out var elapsedMs),
            Is.True);
        Assert.That(elapsedMs, Is.GreaterThanOrEqualTo(0));
    }
}
