
using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Validators.Categories;

namespace LibraryManagement.UnitTests.Validators.Categories;

[TestFixture]
internal class CreateCategoryDtoValidatorTests
{
    private readonly CreateCategoryDtoValidator _cut = new();

    [Test]
    public async Task ShouldReturnTrue_IfNameIsCorrect()
    {
        var dto = new CreateCategoryDto { Name = "Fiction" };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task ShouldReturnFalse_IfNameIsEmpty()
    {
        var dto = new CreateCategoryDto { Name = string.Empty };

        var result =  _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ShouldReturnFalse_IfNameIsTooLong()
    {
        var dto = new CreateCategoryDto
        {
            Name = "VeryLongName_________________________________________________________________________________"
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
    }
}
