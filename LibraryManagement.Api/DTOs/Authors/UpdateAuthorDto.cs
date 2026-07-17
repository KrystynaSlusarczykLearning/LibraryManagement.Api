using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.DTOs.Authors;

public record UpdateAuthorDto
{
    [Required]
    [MaxLength(50)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(50)]
    public required string LastName { get; init; }

    [Range(-2000, 2026)]
    public int? YearOfBirth { get; init; }
}
