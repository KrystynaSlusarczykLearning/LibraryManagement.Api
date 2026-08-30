using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.RequestParameters;

public class PaginationParameters
{
    private const int MinimalPageNumber = 1;
    public const int DefaultPageNumber = 1;
    private const int MinimalPageSize = 1;
    private const int MaximalPageSize = 50;
    public const int DefaultPageSize = 10;

    private int _pageNumber = DefaultPageNumber;
    [FromQuery(Name = "pageNumber")]
    public int PageNumber 
    {
        get => _pageNumber; 
        set => _pageNumber = value < MinimalPageNumber ? MinimalPageNumber : value; 
    }

    private int _pageSize = DefaultPageSize;
    [FromQuery(Name = "pageSize")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = 
            value < MinimalPageSize ? MinimalPageSize : 
            value > MaximalPageSize ? MaximalPageSize : value;
    }
}
