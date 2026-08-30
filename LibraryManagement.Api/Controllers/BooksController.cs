using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(
    LibraryDbContext dbContext,
    BookMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadBookDto>> GetById(int id)
    {
        var book = await dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors)
            .FirstOrDefaultAsync(book => book.Id == id);

        if(book is null)
        {
            return Problem(
                detail: $"A book with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(mapper.ToReadDto(book));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ReadBookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReadBookDto>>> GetAll(
        [FromQuery] PaginationParameters pagination,
        [FromQuery] BookFilteringParameters filteringParameters,
        [FromQuery] SortingParameters<BookSortBy> sortingParameters)
    {
        var query = dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors)
            .AsQueryable();

        if(filteringParameters.Title is not null)
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

        query = sortingParameters.SortBy switch
        {
            BookSortBy.Title => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(book => book.Title) :
                query.OrderByDescending(book => book.Title),
            BookSortBy.AvailableCopies => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(book => book.AvailableCopies) :
                query.OrderByDescending(book => book.AvailableCopies),
            _ => query.OrderBy(book => book.Id)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);

        var books = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var readDtos = books
            .Select(mapper.ToReadDto)
            .ToList();

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Total-Pages", totalPages.ToString());

        return Ok(readDtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReadBookDto>> Create(CreateBookDto createDto)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == createDto.CategoryId);

        if (category is null)
        {
            return Problem(
                detail: $"Category with Id {createDto.CategoryId} does not exist.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var uniqueAuthorIds = createDto.AuthorIds.Distinct().ToList();

        var authors = await dbContext.Authors
            .Where(author => uniqueAuthorIds.Contains(author.Id))
            .ToListAsync();

        if(authors.Count != uniqueAuthorIds.Count)
        {
            return Problem(
                detail: "One or more of the provided author ids is not valid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var book = mapper.ToEntity(createDto);
        book.AvailableCopies = book.TotalCopies;
        book.Category = category;
        book.Authors = authors;

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = book.Id },
            mapper.ToReadDto(book));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadBookDto>> Update(int id, UpdateBookDto updateDto)
    {
        var existingBook = await dbContext.Books
            .Include(book => book.Authors)
            .FirstOrDefaultAsync(book => book.Id == id);

        if (existingBook is null)
        {
            return Problem(
                detail: $"A book with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == updateDto.CategoryId);

        if (category is null)
        {
            return Problem(
                detail: $"Category with Id {updateDto.CategoryId} does not exist.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var uniqueAuthorIds = updateDto.AuthorIds.Distinct().ToList();

        var authors = await dbContext.Authors
            .Where(author => uniqueAuthorIds.Contains(author.Id))
            .ToListAsync();

        if (authors.Count != uniqueAuthorIds.Count)
        {
            return Problem(
                detail: "One or more of the provided author ids is not valid.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var totalCopiesBeforeEntityUpdate = existingBook.TotalCopies;
        UpdateAvailableCopies(
            totalCopiesBeforeEntityUpdate,
            updateDto.TotalCopies, 
            existingBook);

        mapper.UpdateEntity(updateDto, existingBook);
        existingBook.Category = category;
        existingBook.Authors = authors;

        await dbContext.SaveChangesAsync();

        return Ok(mapper.ToReadDto(existingBook));
    }

    private static void UpdateAvailableCopies(
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
            // TODO: once Loans are implemented, add a check here to ensure
            // that TotalCopies cannot be reduced below the number of active loans

            existingBook.AvailableCopies += totalCopiesDelta;
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        var bookToBeDeleted = await dbContext.Books.FindAsync(id);

        if (bookToBeDeleted is null)
        {
            return Problem(
                detail: $"A book with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        dbContext.Books.Remove(bookToBeDeleted);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
