using LibraryManagement.Api.Entities;

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

        group.MapGet("/", GetAll);
    }

    private static async Task<IResult> GetAll()
    {
        var categories = _categories;
        return Results.Ok(categories);
    }
}
