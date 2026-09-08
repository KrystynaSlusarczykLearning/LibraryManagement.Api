using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;

namespace LibraryManagement.Api.Services.Contracts;

public interface IBooksService
{
    Task<ErrorOr<PagedResult<ReadBookDto>>> GetAllAsync(
        PaginationParameters pagination, 
        BookFilteringParameters filteringParameters,
        SortingParameters<BookSortBy> sortingParameters);

    Task<ErrorOr<ReadBookDto>> CreateAsync(CreateBookDto createDto);
}
