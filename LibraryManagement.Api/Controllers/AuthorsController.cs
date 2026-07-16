using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.DTOs.Authors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagement.Api.Mapping;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController(
    LibraryDbContext dbContext,
    AuthorMapper mapper) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReadAuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadAuthorDto>> GetById(int id)
    {
        var author = await dbContext.Authors.FindAsync(id);

        if(author is null)
        {
            return Problem(
                detail: $"An author with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(mapper.ToReadDto(author));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ReadAuthorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReadAuthorDto>>> GetAll()
    {
        var authors = await dbContext.Authors.ToListAsync();

        var readDtos = authors
            .Select(mapper.ToReadDto)
            .ToList();

        return Ok(readDtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReadAuthorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReadAuthorDto>> Create(CreateAuthorDto createDto)
    {
        if(string.IsNullOrWhiteSpace(createDto.FirstName) ||
           string.IsNullOrWhiteSpace(createDto.LastName))
        {
            return Problem(
                detail: $"Author's first name and last name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var author = mapper.ToEntity(createDto);

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = author.Id },
            mapper.ToReadDto(author));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReadAuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadAuthorDto>> Update(int id, UpdateAuthorDto updateDto)
    {
        if (string.IsNullOrWhiteSpace(updateDto.FirstName) ||
            string.IsNullOrWhiteSpace(updateDto.LastName))
        {
            return Problem(
                detail: "Author's first name and last name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var existingAuthor = await dbContext.Authors.FindAsync(id);

        if (existingAuthor is null)
        {
            return Problem(
                detail: $"An author with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        mapper.UpdateEntity(updateDto, existingAuthor);

        await dbContext.SaveChangesAsync();

        return Ok(mapper.ToReadDto(existingAuthor));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        var authorToBeDeleted = await dbContext.Authors.FindAsync(id);

        if (authorToBeDeleted is null)
        {
            return Problem(
                detail: $"An author with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        dbContext.Authors.Remove(authorToBeDeleted);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
