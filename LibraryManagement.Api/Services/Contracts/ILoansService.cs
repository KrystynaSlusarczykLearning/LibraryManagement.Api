using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;

namespace LibraryManagement.Api.Services.Contracts;

public interface ILoansService
{
    Task<ErrorOr<ReadLoanDto>> GetByIdAsync(int id);

    Task<ErrorOr<PagedResult<ReadLoanDto>>> GetAllAsync(
        PaginationParameters pagination,
        LoanFilteringParameters filteringParameters,
        SortingParameters<LoanSortBy> sortingParameters);

    Task<ErrorOr<ReadLoanDto>> CreateAsync(CreateLoanDto createDto);

    Task<ErrorOr<ReadLoanDto>> ReturnAsync(int id);
}