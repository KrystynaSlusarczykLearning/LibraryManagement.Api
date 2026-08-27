namespace LibraryManagement.Api.DTOs.Books;

public class BookAuthorDto
{
    public required int Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
