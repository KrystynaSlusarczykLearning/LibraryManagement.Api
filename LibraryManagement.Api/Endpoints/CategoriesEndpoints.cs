using ErrorOr;
using FluentValidation;
using LibraryManagement.Api.Common;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Endpoints;

public static class CategoriesEndpoints
{
    public static void MapCategoryEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetCategoryById")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAll);

        group.MapGet("/{id:int}/books", GetBooksByCategory)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateCategoryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateCategoryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", Delete)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<ReadCategoryDto>, ProblemHttpResult>> GetById(
        int id,
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        var category = await dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        return TypedResults.Ok(mapper.ToReadDto(category));
    }

    private static async Task<Ok<List<ReadCategoryDto>>> GetAll(
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        var categories = await dbContext
            .Categories
            .OrderBy(category => category.Name)
            .ToListAsync();

        var readDtos = categories
            .Select(mapper.ToReadDto)
            .ToList();

        return TypedResults.Ok(readDtos);
    }

    private static async Task<Results<Ok<List<ReadBookDto>>, ProblemHttpResult>> GetBooksByCategory(
        int id,
        LibraryDbContext dbContext,
        HttpContext httpContext,
        BookMapper mapper,
        int pageNumber = PaginationParameters.DefaultPageNumber,
        int pageSize = PaginationParameters.DefaultPageSize)
    {
        var pagination = new PaginationParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var category = await dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        var query = dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors.OrderBy(author => author.Id))
            .Where(book => book.CategoryId == id)
            .OrderBy(book => book.Id);

        var pagedResult = await query.ToPagedResult(
            pagination,
            book => mapper.ToReadDto(book));

        httpContext.Response.AppendPaginationHeaders(pagedResult);

        return TypedResults.Ok(pagedResult.Items);
    }

    private static async Task<Results<CreatedAtRoute<ReadCategoryDto>, ProblemHttpResult>> Create(
        CreateCategoryDto createDto,
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        if (await dbContext.Categories.AnyAsync(
            existingCategory => existingCategory.Name == createDto.Name))
        {
            return CategoryErrors.NameAlreadyTaken(createDto.Name);
        }

        var category = mapper.ToEntity(createDto);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        return TypedResults.CreatedAtRoute(
            mapper.ToReadDto(category),
            "GetCategoryById",
            new { id = category.Id });
    }

    private static async Task<Results<Ok<ReadCategoryDto>, ProblemHttpResult>> Update(
        int id,
        UpdateCategoryDto updateDto,
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        var existingCategory = await dbContext.Categories.FindAsync(id);

        if (existingCategory is null)
        {
            return CategoryErrors.NotFound(id);
        }

        if (await dbContext.Categories.AnyAsync(category =>
            category.Id != id &&
            category.Name == updateDto.Name))
        {
            return CategoryErrors.NameAlreadyTaken(updateDto.Name);
        }

        mapper.UpdateEntity(updateDto, existingCategory);

        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(mapper.ToReadDto(existingCategory));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        LibraryDbContext dbContext)
    {
        var categoryToBeDeleted = await dbContext.Categories.FindAsync(id);

        if (categoryToBeDeleted is null)
        {
            return CategoryErrors.NotFound(id);
        }

        var hasBooks = await dbContext.Books.AnyAsync(book => book.CategoryId == id);

        if(hasBooks)
        {
            return CategoryErrors.HasBooksAssigned(id);
        }

        dbContext.Categories.Remove(categoryToBeDeleted);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}