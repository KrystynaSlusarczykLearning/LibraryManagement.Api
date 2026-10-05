using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Mapping;

namespace LibraryManagement.UnitTests.Mapping;

[TestFixture]
internal class MemberMapperTests
{
    private readonly MemberMapper _cut = new();
    private const string SampleUserId = "bfcef2a7-a309-4345-8a69-d1234500f056";

    [TestCase(true)]
    [TestCase(false)]
    public void ToReadDto_ShouldMapAllProperties(bool isActive)
    {
        var member = new Member
        {
            Id = 7,
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = isActive,
            UserId = SampleUserId
        };

        var result = _cut.ToReadDto(member);

        Assert.That(result, Is.EqualTo(new ReadMemberDto
        {
            Id = 7,
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = isActive
        }));
    }

    [Test]
    public void UpdateEntity_ShouldUpdateNames_AndKeepIdActiveStatusAndLoans()
    {
        var loan = new Loan
        {
            BookId = 1,
            MemberId = 7,
            LoanDate = new DateOnly(2026, 1, 1),
            DueDate = new DateOnly(2026, 1, 15)
        };
        var member = new Member
        {
            Id = 7,
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = true,
            Loans = [loan],
            UserId = SampleUserId
        };
        var dto = new UpdateMemberDto { FirstName = "John", LastName = "Brown" };

        _cut.UpdateEntity(dto, member);

        Assert.That(member.FirstName, Is.EqualTo("John"));
        Assert.That(member.LastName, Is.EqualTo("Brown"));
        Assert.That(member.Id, Is.EqualTo(7));
        Assert.That(member.IsActive, Is.True);
        Assert.That(member.Loans, Is.EqualTo([loan]));
    }
}
