using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Mapping;

namespace LibraryManagement.UnitTests.Mapping;

[TestFixture]
internal class MemberMapperTests
{
    private readonly MemberMapper _cut = new();

    [TestCase(true)]
    [TestCase(false)]
    public void ToReadDto_ShouldMapAllProperties(bool isActive)
    {
        var member = new Member
        {
            Id = 7,
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = isActive
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
    public void ToEntity_ShouldMapNames_AndLeaveOtherPropertiesAtDefaults()
    {
        var dto = new CreateMemberDto { FirstName = "Jane", LastName = "Smith" };

        var result = _cut.ToEntity(dto);

        Assert.That(result.FirstName, Is.EqualTo("Jane"));
        Assert.That(result.LastName, Is.EqualTo("Smith"));
        Assert.That(result.Id, Is.Zero);
        Assert.That(result.IsActive, Is.False);
        Assert.That(result.Loans, Is.Empty);
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
            Loans = [loan]
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
