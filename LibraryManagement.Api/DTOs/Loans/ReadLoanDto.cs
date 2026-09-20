using LibraryManagement.Api.Entities;

namespace LibraryManagement.Api.DTOs.Loans;

public record ReadLoanDto
{
    public required int Id { get; init; }
    public required int BookId { get; init; }
    public required string BookTitle { get; init; }
    public required int MemberId { get; init; }
    public required string MemberFirstName { get; init; }
    public required string MemberLastName { get; init; }
    public required DateOnly LoanDate { get; init; }
    public required DateOnly DueDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public required LoanStatus Status { get; init; }
}
