using LibraryManagement.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace LibraryManagement.UnitTests.Middleware;

[TestFixture]
internal class GlobalExceptionHandlerTests
{
    private const string ExceptionMessage = "Connection string: Host=secret";

    private readonly GlobalExceptionHandler _cut =
        new(NullLogger<GlobalExceptionHandler>.Instance);

    [Test]
    public async Task ShouldHandleException_AndReturnInternalServerError()
    {
        var httpContext = CreateHttpContext();

        var handled = await _cut.TryHandleAsync(
            httpContext, new InvalidOperationException(ExceptionMessage), CancellationToken.None);

        Assert.That(handled, Is.True);
        Assert.That(httpContext.Response.StatusCode,
            Is.EqualTo(StatusCodes.Status500InternalServerError));
    }

    [Test]
    public async Task ShouldWriteGenericProblemDetails_WithoutExceptionDetails()
    {
        var httpContext = CreateHttpContext();

        await _cut.TryHandleAsync(
            httpContext, new InvalidOperationException(ExceptionMessage), CancellationToken.None);

        var problemDetails = await ReadProblemDetailsAsync(httpContext);

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails.Status, Is.EqualTo(StatusCodes.Status500InternalServerError));
        Assert.That(problemDetails.Title, Is.EqualTo("Internal Server Error"));
        Assert.That(problemDetails.Detail, Does.Not.Contain(ExceptionMessage));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }

    private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;

        return await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            JsonSerializerOptions.Web);
    }
}
