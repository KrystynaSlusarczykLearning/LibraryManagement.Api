using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Api.DTOs.Users;

public class RegisterMemberUserDto
{
    [Required]
    [MaxLength(50)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(50)]
    public required string LastName { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
