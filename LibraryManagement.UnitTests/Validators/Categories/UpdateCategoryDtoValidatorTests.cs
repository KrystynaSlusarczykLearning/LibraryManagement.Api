using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Validators.Categories;

namespace LibraryManagement.UnitTests.Validators.Categories;

[TestFixture]
internal class UpdateCategoryDtoValidatorTests
{
    private readonly UpdateCategoryDtoValidator _cut = new();

    [Test]
    public async Task ShouldReturnTrue_IfNameIsCorrect()
    {
        var dto = new UpdateCategoryDto { Name = "Fiction" };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task ShouldReturnFalse_IfNameIsEmpty()
    {
        var dto = new UpdateCategoryDto { Name = string.Empty };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ShouldReturnFalse_IfNameIsTooLong()
    {
        var dto = new UpdateCategoryDto
        {
            Name = "VeryLongName_________________________________________________________________________________"
        };

        var result = _cut.Validate(dto);

        Assert.That(result.IsValid, Is.False);
    }
}
