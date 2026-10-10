using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.DTOs.Users;

public record LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
