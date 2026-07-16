namespace LibraryManagement.Api.DTOs.Authors;

public record ReadAuthorDto
{
    public required int Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int? YearOfBirth { get; init; }
}
