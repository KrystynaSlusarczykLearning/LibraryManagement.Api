using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class AuthorsService(
    LibraryDbContext dbContext,
    AuthorMapper mapper) : IAuthorsService
{
    public async Task<ErrorOr<ReadAuthorDetailDto>> GetByIdAsync(int id)
    {
        var author = await dbContext.Authors
            .Include(author => author.Books)
            .FirstOrDefaultAsync(author => author.Id == id);

        if (author is null)
        {
            return AuthorErrors.AuthorNotFound(id);
        }

        return mapper.ToReadDetailDto(author);
    }

    public async Task<ErrorOr<PagedResult<ReadAuthorDto>>> GetAllAsync(
        PaginationParameters pagination)
    {
        var query = dbContext.Authors
            .OrderBy(author => author.Id)
            .AsQueryable();

        return await query.ToPagedResult(
            pagination,
            author => mapper.ToReadDto(author));
    }

    public async Task<ErrorOr<ReadAuthorDetailDto>> CreateAsync(CreateAuthorDto createDto)
    {
        var author = mapper.ToEntity(createDto);

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync();

        return mapper.ToReadDetailDto(author);
    }

    public async Task<ErrorOr<ReadAuthorDetailDto>> UpdateAsync(
        int id,
        UpdateAuthorDto updateDto)
    {
        var existingAuthor = await dbContext.Authors
            .Include(author => author.Books)
            .FirstOrDefaultAsync(author => author.Id == id);

        if (existingAuthor is null)
        {
            return AuthorErrors.AuthorNotFound(id);
        }

        mapper.UpdateEntity(updateDto, existingAuthor);

        await dbContext.SaveChangesAsync();

        return mapper.ToReadDetailDto(existingAuthor);
    }

    public async Task<ErrorOr<ReadAuthorDetailDto>> PatchAsync(
        int id,
        JsonPatchDocument<PatchAuthorDto> patchDocument)
    {
        var existingAuthor = await dbContext.Authors
            .Include(author => author.Books)
            .FirstOrDefaultAsync(author => author.Id == id);

        if (existingAuthor is null)
        {
            return AuthorErrors.AuthorNotFound(id);
        }

        var patchDto = mapper.ToPatchDto(existingAuthor);
        patchDocument.ApplyTo(patchDto);
        mapper.UpdateEntity(patchDto, existingAuthor);

        await dbContext.SaveChangesAsync();

        return mapper.ToReadDetailDto(existingAuthor);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id)
    {
        var authorToBeDeleted = await dbContext.Authors
            .Include(author => author.Books)
            .FirstOrDefaultAsync(author => author.Id == id);

        if (authorToBeDeleted is null)
        {
            return AuthorErrors.AuthorNotFound(id);
        }

        if (authorToBeDeleted.Books.Count != 0)
        {
            return AuthorErrors.AuthorHasBooks(id);
        }

        dbContext.Authors.Remove(authorToBeDeleted);
        await dbContext.SaveChangesAsync();

        return Result.Deleted;
    }
}
