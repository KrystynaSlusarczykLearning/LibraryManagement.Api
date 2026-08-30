using LibraryManagement.Api.RequestParameters.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.RequestParameters;

public record SortingParameters<TSortBy> where TSortBy : struct, Enum
{
    [FromQuery(Name = "sortBy")]
    public TSortBy? SortBy { get; init; } = null;

    [FromQuery(Name = "sortDirection")]
    public SortDirection SortDirection { get; init; } = Enums.SortDirection.Asc;
}
