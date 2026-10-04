namespace LibraryManagement.Api.Entities;

public class Member
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Loan> Loans { get; set; } = [];
    public string UserId { get; set; } //TODO make it required after removing CreateMemberDto
}
