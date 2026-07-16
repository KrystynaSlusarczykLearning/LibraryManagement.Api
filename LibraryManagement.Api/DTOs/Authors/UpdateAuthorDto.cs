namespace LibraryManagement.Api.DTOs.Authors;

public record UpdateAuthorDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public int? YearOfBirth { get; init; }
}
