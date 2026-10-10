using LibraryManagement.Api.Common;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[Authorize(Roles = Roles.Librarian)]
[ApiController]
[Route("api/[controller]")]
public class BooksController(
    IBooksService booksService) : WebApiController
{
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadBookDto>> GetById(int id)
    {
        var result = await booksService.GetByIdAsync(id);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(List<ReadBookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReadBookDto>>> GetAll(
        [FromQuery] PaginationParameters pagination,
        [FromQuery] BookFilteringParameters filteringParameters,
        [FromQuery] SortingParameters<BookSortBy> sortingParameters)
    {
        var result = await booksService.GetAllAsync(
            pagination,
            filteringParameters,
            sortingParameters);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        Response.AppendPaginationHeaders(result.Value);
        return Ok(result.Value.Items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReadBookDto>> Create(CreateBookDto createDto)
    {
        var result = await booksService.CreateAsync(createDto);

        if(result.IsError)
        {
            return Problem(result.FirstError);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReadBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadBookDto>> Update(int id, UpdateBookDto updateDto)
    {
        var result = await booksService.UpdateAsync(id, updateDto);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await booksService.DeleteAsync(id);

        if(result.IsError)
        {
            return Problem(result.FirstError);
        }

        return NoContent();
    }
}
