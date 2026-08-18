using FluentValidation;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Mapping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
            .AddEndpointFilter<ValidationFilter<CreateCategoryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateCategoryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", Delete)
            .ProducesProblem(StatusCodes.Status404NotFound);
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

        dbContext.Categories.Remove(categoryToBeDeleted);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}