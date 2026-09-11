namespace LibraryManagement.Api.DTOs.Members;

public record ReadMemberDto
{
    public required int Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required bool IsActive { get; init; }
}
