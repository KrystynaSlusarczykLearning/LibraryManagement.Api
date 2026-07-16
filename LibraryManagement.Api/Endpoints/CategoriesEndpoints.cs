using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Entities;
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

        group.MapPost("/", Create)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Update)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", Delete)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<ReadCategoryDto>, ProblemHttpResult>> GetById(
        int id,
        LibraryDbContext dbContext)
    {
        var category = await dbContext.Categories.FindAsync(id);

        if(category is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var readDto = new ReadCategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return TypedResults.Ok(readDto);
    }

    private static async Task<Ok<List<ReadCategoryDto>>> GetAll(
        LibraryDbContext dbContext)
    {
        var categories = await dbContext.Categories.ToListAsync();

        var readDtos = categories.Select(category =>
            new ReadCategoryDto
            {
                Id = category.Id,
                Name = category.Name
            }).ToList();

        return TypedResults.Ok(readDtos);
    }

    private static async Task<Results<CreatedAtRoute<ReadCategoryDto>, ProblemHttpResult>> Create(
        CreateCategoryDto createDto,
        LibraryDbContext dbContext)
    {
        if(string.IsNullOrWhiteSpace(createDto.Name))
        {
            return TypedResults.Problem(
                detail: $"A category name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (await dbContext.Categories.AnyAsync(
            existingCategory => existingCategory.Name == createDto.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with Name {createDto.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var category = new Category
        {
            Name = createDto.Name
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var readDto = new ReadCategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return TypedResults.CreatedAtRoute(
            readDto,
            "GetCategoryById",
            new { id = category.Id });
    }

    private static async Task<Results<Ok<ReadCategoryDto>, ProblemHttpResult>> Update(
        int id,
        UpdateCategoryDto updateDto,
        LibraryDbContext dbContext)
    {
        if(string.IsNullOrWhiteSpace(updateDto.Name))
        {
            return TypedResults.Problem(
                detail: "Category name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var existingCategory = await dbContext.Categories.FindAsync(id);

        if(existingCategory is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if(await dbContext.Categories.AnyAsync(category =>
            category.Id != id &&
            category.Name == updateDto.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with name {updateDto.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        existingCategory.Name = updateDto.Name;
        await dbContext.SaveChangesAsync();

        var readDto = new ReadCategoryDto
        {
            Id = existingCategory.Id,
            Name = existingCategory.Name
        };

        return TypedResults.Ok(readDto);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        LibraryDbContext dbContext)
    {
        var categoryToBeDeleted = await dbContext.Categories.FindAsync(id);

        if(categoryToBeDeleted is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        dbContext.Categories.Remove(categoryToBeDeleted);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
