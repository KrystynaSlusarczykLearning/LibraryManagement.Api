using LibraryManagement.Api.Entities;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;

namespace LibraryManagement.Api.Extensions;

public static class BookQueryableExtensions
{
    public static IQueryable<Book> ApplyFiltering(
        this IQueryable<Book> query,
        BookFilteringParameters filteringParameters)
    {
        if (filteringParameters.Title is not null)
        {
            query = query.Where(
                book => book.Title.Contains(filteringParameters.Title));
        }

        if (filteringParameters.CategoryId is not null)
        {
            query = query.Where(
                book => book.CategoryId == filteringParameters.CategoryId);
        }

        if (filteringParameters.AuthorId is not null)
        {
            query = query.Where(
                book => book.Authors.Any(
                    author => author.Id == filteringParameters.AuthorId));
        }

        return query;
    }

    public static IOrderedQueryable<Book> ApplySorting(
        this IQueryable<Book> query,
        SortingParameters<BookSortBy> sortingParameters)
    {
        IOrderedQueryable<Book> orderedQuery = sortingParameters.SortBy switch
        {
            BookSortBy.Title => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(book => book.Title) :
                query.OrderByDescending(book => book.Title),
            BookSortBy.AvailableCopies => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(book => book.AvailableCopies) :
                query.OrderByDescending(book => book.AvailableCopies),
            _ => query.OrderBy(book => book.Id)
        };
        orderedQuery = orderedQuery.ThenBy(book => book.Id);

        return orderedQuery;
    }
}
