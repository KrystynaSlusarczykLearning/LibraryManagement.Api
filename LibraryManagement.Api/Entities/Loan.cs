namespace LibraryManagement.Api.Entities;

public class Loan
{
    public int Id { get; set; }
    public required int BookId { get; set; }
    public Book? Book { get; set; }
    public required int MemberId { get; set; }
    public Member? Member { get; set; }
    public required DateOnly LoanDate { get; set; }
    public required DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    public LoanStatus Status => ReturnDate.HasValue
        ? LoanStatus.Returned
        : DueDate < DateOnly.FromDateTime(DateTime.Today)
            ? LoanStatus.Overdue
            : LoanStatus.Active;
            
}
