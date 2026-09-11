namespace LibraryManagement.Api.Entities;

public class Member
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; }
}
