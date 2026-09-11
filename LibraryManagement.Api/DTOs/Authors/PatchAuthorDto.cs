using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.DTOs.Authors;

public record PatchAuthorDto
{
    [MinLength(1)]
    [MaxLength(50)]
    public string? FirstName { get; init; }

    [MinLength(1)]
    [MaxLength(50)]
    public string? LastName { get; init; }

    [Range(-2000, 2026)]
    public int? YearOfBirth { get; init; }
}
