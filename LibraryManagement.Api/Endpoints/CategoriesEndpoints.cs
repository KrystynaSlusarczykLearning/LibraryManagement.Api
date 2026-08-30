using FluentValidation;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Categories;
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
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(mapper.ToReadDto(category));
    }

    private static async Task<Ok<List<ReadCategoryDto>>> GetAll(
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        var categories = await dbContext.Categories.ToListAsync();

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
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var query = dbContext.Books
            .Include(book => book.Category)
            .Include(book => book.Authors)
            .Where(book => book.CategoryId == id)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);

        var books = await query
            .OrderBy(book => book.Id)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var readDtos = books
            .Select(mapper.ToReadDto)
            .ToList();

        httpContext.Response.Headers.Append("X-Total-Count", totalCount.ToString());
        httpContext.Response.Headers.Append("X-Total-Pages", totalPages.ToString());

        return TypedResults.Ok(readDtos);
    }

    private static async Task<Results<CreatedAtRoute<ReadCategoryDto>, ProblemHttpResult>> Create(
        CreateCategoryDto createDto,
        LibraryDbContext dbContext,
        CategoryMapper mapper)
    {
        if (await dbContext.Categories.AnyAsync(
            existingCategory => existingCategory.Name == createDto.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with Name {createDto.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
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
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (await dbContext.Categories.AnyAsync(category =>
            category.Id != id &&
            category.Name == updateDto.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with name {updateDto.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
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
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var hasBooks = await dbContext.Books.AnyAsync(book => book.CategoryId == id);

        if(hasBooks)
        {
            return TypedResults.Problem(
                detail: $"Cannot delete the category with Id {id}, " +
                        $"because it has books assigned. " +
                        $"Please remove or reassign the books first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        dbContext.Categories.Remove(categoryToBeDeleted);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}