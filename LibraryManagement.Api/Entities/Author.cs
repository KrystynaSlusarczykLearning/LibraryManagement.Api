namespace LibraryManagement.Api.Entities;

public class Author
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int? YearOfBirth { get; set; }
    public ICollection<Book> Books { get; set; } = [];
}

