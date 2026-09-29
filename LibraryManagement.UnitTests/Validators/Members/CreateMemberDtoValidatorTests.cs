using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Validators.Members;

namespace LibraryManagement.UnitTests.Validators.Members;

[TestFixture]
internal class CreateMemberDtoValidatorTests
{
    private const string ValidFirstName = "Jane";
    private const string ValidLastName = "Smith";
    private const int MaximalNameLength = 50;

    private readonly CreateMemberDtoValidator _cut = new();

    [Test]
    public void ShouldReturnTrue_IfNamesAreCorrect()
    {
        var dto = new CreateMemberDto
        {
            FirstName = ValidFirstName,
            LastName = ValidLastName
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ShouldReturnTrue_IfNamesHaveMaximalLength()
    {
        var dto = new CreateMemberDto
        {
            FirstName = new string('a', MaximalNameLength),
            LastName = new string('b', MaximalNameLength)
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ShouldReturnFalse_IfFirstNameIsEmpty(string firstName)
    {
        var dto = new CreateMemberDto
        {
            FirstName = firstName,
            LastName = ValidLastName
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(
            result.Errors.Select(error => error.PropertyName),
            Is.EquivalentTo([nameof(CreateMemberDto.FirstName)]));
    }

    [Test]
    public void ShouldReturnFalse_IfFirstNameIsTooLong()
    {
        var dto = new CreateMemberDto
        {
            FirstName = new string('a', MaximalNameLength + 1),
            LastName = ValidLastName
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(
            result.Errors.Select(error => error.PropertyName),
            Is.EquivalentTo([nameof(CreateMemberDto.FirstName)]));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ShouldReturnFalse_IfLastNameIsEmpty(string lastName)
    {
        var dto = new CreateMemberDto
        {
            FirstName = ValidFirstName,
            LastName = lastName
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(
            result.Errors.Select(error => error.PropertyName),
            Is.EquivalentTo([nameof(CreateMemberDto.LastName)]));
    }

    [Test]
    public void ShouldReturnFalse_IfLastNameIsTooLong()
    {
        var dto = new CreateMemberDto
        {
            FirstName = ValidFirstName,
            LastName = new string('b', MaximalNameLength + 1)
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(
            result.Errors.Select(error => error.PropertyName),
            Is.EquivalentTo([nameof(CreateMemberDto.LastName)]));
    }

    [Test]
    public void ShouldReportBothProperties_IfBothNamesAreInvalid()
    {
        var dto = new CreateMemberDto
        {
            FirstName = string.Empty,
            LastName = string.Empty
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
        Assert.That(
            result.Errors.Select(error => error.PropertyName),
            Is.EquivalentTo([
                nameof(CreateMemberDto.FirstName),
                nameof(CreateMemberDto.LastName)]));
    }
}
