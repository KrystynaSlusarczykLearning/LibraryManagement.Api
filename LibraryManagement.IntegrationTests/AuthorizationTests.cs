using LibraryManagement.IntegrationTests.Utils;
using System.Net;

namespace LibraryManagement.IntegrationTests;

[TestFixture]
internal class AuthorizationTests : IntegrationTestsBase
{
    protected override string DatabaseName => nameof(AuthorizationTests);

    [TestCaseSource(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenNotAuthenticated(
        HttpMethod httpMethod, string url)
    {
        var anonymousClient = Factory.CreateClient();

        var response = await anonymousClient.SendAsync(
            new HttpRequestMessage(httpMethod, url));

        Assert.That(
            response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCaseSource(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_ShouldReturnForbidden_WhenAuthenticatedAsMember(
        HttpMethod httpMethod, string url)
    {
        const string MemberEmail = "member@test.com";
        const string Password = "Pwd123!";
        var member = await TestDataFactory.CreateMemberAsync(
            email: MemberEmail, password: Password);
        var memberClient = await CreateAuthenticatedClientAsync(MemberEmail, Password);

        var response = await memberClient.SendAsync(
            new HttpRequestMessage(httpMethod, url));

        Assert.That(
            response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static IEnumerable<TestCaseData> ProtectedEndpoints()
    {
        // Categories
        yield return new TestCaseData(HttpMethod.Post, "/api/categories");
        yield return new TestCaseData(HttpMethod.Put, "/api/categories/1");
        yield return new TestCaseData(HttpMethod.Delete, "/api/categories/1");

        // Books
        yield return new TestCaseData(HttpMethod.Post, "/api/books");
        yield return new TestCaseData(HttpMethod.Put, "/api/books/1");
        yield return new TestCaseData(HttpMethod.Delete, "/api/books/1");

        // Authors
        yield return new TestCaseData(HttpMethod.Post, "/api/authors");
        yield return new TestCaseData(HttpMethod.Put, "/api/authors/1");
        yield return new TestCaseData(HttpMethod.Patch, "/api/authors/1");
        yield return new TestCaseData(HttpMethod.Delete, "/api/authors/1");

        // Members
        yield return new TestCaseData(HttpMethod.Get, "/api/members");
        yield return new TestCaseData(HttpMethod.Get, "/api/members/1");
        yield return new TestCaseData(HttpMethod.Put, "/api/members/1");
        yield return new TestCaseData(HttpMethod.Delete, "/api/members/1");
        yield return new TestCaseData(HttpMethod.Patch, "/api/members/1/restore");

        // Loans
        yield return new TestCaseData(HttpMethod.Get, "/api/loans");
        yield return new TestCaseData(HttpMethod.Get, "/api/loans/1");
        yield return new TestCaseData(HttpMethod.Post, "/api/loans");
        yield return new TestCaseData(HttpMethod.Patch, "/api/loans/1/return");

        // Authentication
        yield return new TestCaseData(HttpMethod.Post, "/api/authentication/register-librarian");

    }
}
