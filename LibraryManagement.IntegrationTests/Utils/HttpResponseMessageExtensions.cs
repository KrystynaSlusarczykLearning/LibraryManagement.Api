using LibraryManagement.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace LibraryManagement.IntegrationTests.Utils;

internal static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessStatusCodeWitDetailsAsync(
        this HttpResponseMessage httpResponseMessage)
    {
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            Assert.Fail(
                $"Request failed with status {httpResponseMessage.StatusCode}: {content}");
        }
    }

    public static async Task<T?> ReadPayloadAsync<T>(
        this HttpResponseMessage httpResponseMessage)
    {
        return await httpResponseMessage.Content.ReadFromJsonAsync<T>();
    }

    public static string? GetLocation(this HttpResponseMessage response)
    {
        return response.Headers.Location?.AbsolutePath;
    }

    public static async Task AssertThatProblemDetailsContain(
        this HttpResponseMessage response, string message)
    {
        var problemDetails = await response.ReadPayloadAsync<ProblemDetails>();

        Assert.That(problemDetails, Is.Not.Null);
        Assert.That(problemDetails!.Detail, Does.Contain(message));
    }

    public static async Task AssertThatValidationErrorsContain(
        this HttpResponseMessage response, string propertyName)
    {
        var validationProblem = await response.ReadPayloadAsync<HttpValidationProblemDetails>();

        Assert.That(validationProblem, Is.Not.Null);
        Assert.That(validationProblem!.Errors, Does.ContainKey(propertyName));
    }

    public static string? GetHeaderValue(this HttpResponseMessage response, string key)
    {
        return response.Headers.GetValues(key).FirstOrDefault();
    }

    public static void AssertThatPaginationHeadersAre(
        this HttpResponseMessage response, int totalCount, int totalPages)
    {
        Assert.Multiple(() =>
        {
            Assert.That(response.GetHeaderValue(ResponseHeaders.TotalCount),
                Is.EqualTo(totalCount.ToString()));
            Assert.That(response.GetHeaderValue(ResponseHeaders.TotalPages),
                Is.EqualTo(totalPages.ToString()));
        });
    }
}
