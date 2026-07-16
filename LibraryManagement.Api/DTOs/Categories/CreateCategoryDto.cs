namespace LibraryManagement.Api.DTOs.Categories;

public record CreateCategoryDto
{
    public required string Name { get; init; }
}
