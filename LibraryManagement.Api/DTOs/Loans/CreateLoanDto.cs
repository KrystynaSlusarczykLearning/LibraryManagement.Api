using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.DTOs.Loans;

public record CreateLoanDto
{
    [Required]
    public required int BookId { get; init; }

    [Required]
    public required int MemberId { get; init; }

    [Required]
    public required DateOnly LoanDate { get; init; }

    [Required]
    public required DateOnly DueDate { get; init; }
}