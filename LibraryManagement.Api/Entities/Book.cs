namespace LibraryManagement.Api.Entities;

public class Book
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int AvailableCopies { get; set; }
    public required int TotalCopies { get; set; }
    public required int CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<Author> Authors { get; set; } = [];
}

