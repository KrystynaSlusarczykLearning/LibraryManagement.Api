namespace LibraryManagement.Api.DTOs.Categories;

public record ReadCategoryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}