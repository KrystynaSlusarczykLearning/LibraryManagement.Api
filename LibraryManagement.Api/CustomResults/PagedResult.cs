using LibraryManagement.Api.RequestParameters;

namespace LibraryManagement.Api.CustomResults;

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    PaginationParameters PaginationParameters)
{
    public int TotalPages => 
        (int) Math.Ceiling((double) TotalCount / PaginationParameters.PageSize);

}
