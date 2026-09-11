namespace LibraryManagement.Api.DTOs.Members;

public record UpdateMemberDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
