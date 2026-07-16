using LibraryManagement.Api.DTOs.Categories;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper]
public partial class CategoryMapper
{
    public partial ReadCategoryDto ToReadDto(Category source);

    [MapperIgnoreTarget(nameof(Category.Id))]
    public partial Category ToEntity(CreateCategoryDto source);

    [MapperIgnoreTarget(nameof(Category.Id))]
    public partial void UpdateEntity(UpdateCategoryDto source, Category target);
}

