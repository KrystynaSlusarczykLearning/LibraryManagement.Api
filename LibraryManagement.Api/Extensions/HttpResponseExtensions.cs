using LibraryManagement.Api.Common;
using LibraryManagement.Api.CustomResults;

namespace LibraryManagement.Api.Extensions;

public static class HttpResponseExtensions
{
    public static void AppendPaginationHeaders<T>(
        this HttpResponse response,
        PagedResult<T> pagedResult)
    {
        AppendPaginationHeaders(response, pagedResult.TotalCount, pagedResult.TotalPages);
    }

    public static void AppendPaginationHeaders(
        this HttpResponse response,
        int totalCount, 
        int totalPages)
    {
        response.Headers.Append(ResponseHeaders.TotalCount, totalCount.ToString());
        response.Headers.Append(ResponseHeaders.TotalPages, totalPages.ToString());
    }
}
