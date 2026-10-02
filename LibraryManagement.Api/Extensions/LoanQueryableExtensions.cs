using LibraryManagement.Api.Entities;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;

namespace LibraryManagement.Api.Extensions;

public static class LoanQueryableExtensions
{
    public static IQueryable<Loan> ApplyFiltering(
        this IQueryable<Loan> query,
        LoanFilteringParameters filteringParameters)
    {
        if (filteringParameters.BookId is not null)
        {
            query = query.Where(
                loan => loan.BookId == filteringParameters.BookId);
        }

        if (filteringParameters.MemberId is not null)
        {
            query = query.Where(
                loan => loan.MemberId == filteringParameters.MemberId);
        }

        if (filteringParameters.LoanStatus is not null)
        {
            query = filteringParameters.LoanStatus switch
            {
                LoanStatus.Returned =>
                    query.Where(loan => loan.ReturnDate != null),
                LoanStatus.Overdue =>
                    query.Where(loan => loan.ReturnDate == null &&
                                        loan.DueDate < DateOnly.FromDateTime(DateTime.Today)),
                LoanStatus.Active =>
                    query.Where(loan => loan.ReturnDate == null &&
                                        loan.DueDate >= DateOnly.FromDateTime(DateTime.Today)),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(filteringParameters.LoanStatus),
                    filteringParameters.LoanStatus,
                    "Unsupported loan status.")
            };
        }

        return query;
    }

    public static IOrderedQueryable<Loan> ApplySorting(
        this IQueryable<Loan> query,
        SortingParameters<LoanSortBy> sortingParameters)
    {
        IOrderedQueryable<Loan> orderedQuery = sortingParameters.SortBy switch
        {
            LoanSortBy.DueDate => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(loan => loan.DueDate) :
                query.OrderByDescending(loan => loan.DueDate),
            LoanSortBy.LoanDate => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(loan => loan.LoanDate) :
                query.OrderByDescending(loan => loan.LoanDate),
            _ => query.OrderBy(loan => loan.Id)
        };
        orderedQuery = orderedQuery.ThenBy(loan => loan.Id);

        return orderedQuery;
    }
}
