using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Mapping;

namespace LibraryManagement.UnitTests.Mapping;

[TestFixture]
internal class CategoryMapperTests
{
    private readonly CategoryMapper _cut = new();

    [Test]
    public void ToReadDto_ShouldMapAllProperties()
    {
        var category = new Category { Id = 7, Name = "History" };

        var result = _cut.ToReadDto(category);

        Assert.That(result, Is.EqualTo(new ReadCategoryDto { Id = 7, Name = "History" }));
    }

    [Test]
    public void ToEntity_ShouldMapName_AndLeaveIdAtDefault()
    {
        var dto = new CreateCategoryDto { Name = "History" };

        var result = _cut.ToEntity(dto);

        Assert.That(result.Name, Is.EqualTo("History"));
        Assert.That(result.Id, Is.Zero);
    }

    [Test]
    public void UpdateEntity_ShouldUpdateName_AndKeepId()
    {
        var category = new Category { Id = 7, Name = "History" };
        var dto = new UpdateCategoryDto { Name = "Biography" };

        _cut.UpdateEntity(dto, category);

        Assert.That(category.Name, Is.EqualTo("Biography"));
        Assert.That(category.Id, Is.EqualTo(7));
    }
}
