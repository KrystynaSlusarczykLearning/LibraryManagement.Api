using LibraryManagement.Api.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LibraryManagement.Api.Endpoints;

public static class CategoriesEndpoints
{
    private static List<Category> _categories =
        [
            new Category {Id = 1, Name = "Biography"},
            new Category {Id = 2, Name = "Novel"}
        ];

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

    private static async Task<Results<Ok<Category>, ProblemHttpResult>> GetById(int id)
    {
        var category = _categories.FirstOrDefault(
            category => category.Id == id);

        if(category is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(category);
    }

    private static async Task<Ok<List<Category>>> GetAll()
    {
        var categories = _categories;
        return TypedResults.Ok(categories);
    }

    private static async Task<Results<CreatedAtRoute<Category>, ProblemHttpResult>> Create(
        Category category)
    {
        if(string.IsNullOrWhiteSpace(category.Name))
        {
            return TypedResults.Problem(
                detail: $"A category name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if(_categories.Any(existingCategory => existingCategory.Id == category.Id))
        {
            return TypedResults.Problem(
                detail: $"A category with Id {category.Id} already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (_categories.Any(existingCategory => existingCategory.Name == category.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with Name {category.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        _categories.Add(category);

        return TypedResults.CreatedAtRoute(
            category,
            "GetCategoryById",
            new { id = category.Id });
    }

    private static async Task<Results<Ok<Category>, ProblemHttpResult>> Update(
        int id,
        Category updatedCategory)
    {
        if(string.IsNullOrWhiteSpace(updatedCategory.Name))
        {
            return TypedResults.Problem(
                detail: "Category name cannot be empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var existingCategory = _categories.FirstOrDefault(
            category => category.Id == id);

        if(existingCategory is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if(_categories.Any(category =>
            category.Id != id &&
            category.Name == updatedCategory.Name))
        {
            return TypedResults.Problem(
                detail: $"A category with name {updatedCategory.Name} already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        existingCategory.Name = updatedCategory.Name;

        return TypedResults.Ok(existingCategory);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(int id)
    {
        var categorytoBeDeleted = _categories.FirstOrDefault(
            category => category.Id == id);

        if(categorytoBeDeleted is null)
        {
            return TypedResults.Problem(
                detail: $"A category with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        _categories.Remove(categorytoBeDeleted);

        return TypedResults.NoContent();
    }
}
