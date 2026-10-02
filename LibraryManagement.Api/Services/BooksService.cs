using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class BooksService(
    LibraryDbContext dbContext,
    BookMapper mapper) : IBooksService
{
    public async Task<ErrorOr<ReadBookDto>> GetByIdAsync(int id)
    {
        var book = await dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors.OrderBy(author => author.Id))
            .FirstOrDefaultAsync(book => book.Id == id);

        if (book is null)
        {
            return BookErrors.BookNotFound(id);
        }

        return mapper.ToReadDto(book);
    }

    public async Task<ErrorOr<PagedResult<ReadBookDto>>> GetAllAsync(
        PaginationParameters pagination,
        BookFilteringParameters filteringParameters,
        SortingParameters<BookSortBy> sortingParameters)
    {
        return await dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors.OrderBy(author => author.Id))
            .ApplyFiltering(filteringParameters)
            .ApplySorting(sortingParameters)
            .ToPagedResult(
                pagination,
                book => mapper.ToReadDto(book));
    }

    public async Task<ErrorOr<ReadBookDto>> CreateAsync(CreateBookDto createDto)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == createDto.CategoryId);

        if (category is null)
        {
            return BookErrors.CategoryNotFound(createDto.CategoryId);
        }

        var uniqueAuthorIds = createDto.AuthorIds.Distinct().ToList();

        var authors = await dbContext.Authors
            .Where(author => uniqueAuthorIds.Contains(author.Id))
            .OrderBy(author => author.Id)
            .ToListAsync();

        if (authors.Count != uniqueAuthorIds.Count)
        {
            return BookErrors.AuthorsNotFound();
        }

        var book = mapper.ToEntity(createDto);
        book.AvailableCopies = book.TotalCopies;
        book.Category = category;
        book.Authors = authors;

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync();

        return mapper.ToReadDto(book);
    }

    public async Task<ErrorOr<ReadBookDto>> UpdateAsync(
        int id, 
        UpdateBookDto updateDto)
    {
        var existingBook = await dbContext.Books
            .Include(book => book.Authors)
            .FirstOrDefaultAsync(book => book.Id == id);

        if (existingBook is null)
        {
            return BookErrors.BookNotFound(id);
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == updateDto.CategoryId);

        if (category is null)
        {
            return BookErrors.CategoryNotFound(updateDto.CategoryId);
        }

        var uniqueAuthorIds = updateDto.AuthorIds.Distinct().ToList();

        var authors = await dbContext.Authors
            .Where(author => uniqueAuthorIds.Contains(author.Id))
            .OrderBy(author => author.Id)
            .ToListAsync();

        if (authors.Count != uniqueAuthorIds.Count)
        {
            return BookErrors.AuthorsNotFound();
        }

        var totalCopiesBeforeEntityUpdate = existingBook.TotalCopies;
        var updateCopiesResult = await UpdateAvailableCopies(
            totalCopiesBeforeEntityUpdate,
            updateDto.TotalCopies,
            existingBook);

        if(updateCopiesResult.IsError)
        {
            return updateCopiesResult.FirstError;
        }

        mapper.UpdateEntity(updateDto, existingBook);
        existingBook.Category = category;
        existingBook.Authors = authors;

        await dbContext.SaveChangesAsync();

        return mapper.ToReadDto(existingBook);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id)
    {
        var bookToBeDeleted = await dbContext.Books.FindAsync(id);

        if (bookToBeDeleted is null)
        {
            return BookErrors.BookNotFound(id);
        }

        var hasLoans = await dbContext.Loans.AnyAsync(
            loan => loan.BookId == id);

        if(hasLoans)
        {
            return BookErrors.HasLoans(id);
        }

        dbContext.Books.Remove(bookToBeDeleted);
        await dbContext.SaveChangesAsync();

        return Result.Deleted;
    }

    private async Task<ErrorOr<Success>> UpdateAvailableCopies(
        int oldTotalCopies,
        int newTotalCopies,
        Book existingBook)
    {
        var totalCopiesDelta = newTotalCopies - oldTotalCopies;

        if (totalCopiesDelta > 0)
        {
            existingBook.AvailableCopies += totalCopiesDelta;
        }
        else if (totalCopiesDelta < 0)
        {
            var activeLoansCount = await dbContext.Loans
                .CountAsync(loan => loan.BookId == existingBook.Id &&
                                    loan.ReturnDate == null);

            if (newTotalCopies < activeLoansCount)
            {
                return BookErrors.NotEnoughAvailableCopies(existingBook.Id);
            }

            existingBook.AvailableCopies += totalCopiesDelta;
        }

        return Result.Success;
    }
}
