namespace LibraryManagement.Api.DTOs.Categories;

public record UpdateCategoryDto
{
    public required string Name { get; init; }
}
