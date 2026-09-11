using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AuthorMapper
{
    public partial ReadAuthorDetailDto ToReadDetailDto(Author source);
    public partial ReadAuthorDto ToReadDto(Author source);

    [MapperIgnoreTarget(nameof(Author.Id))]
    [MapperIgnoreTarget(nameof(Author.Books))]
    public partial Author ToEntity(CreateAuthorDto source);

    [MapperIgnoreTarget(nameof(Author.Id))]
    [MapperIgnoreTarget(nameof(Author.Books))]
    public partial void UpdateEntity(UpdateAuthorDto source, Author target);

    public partial PatchAuthorDto ToPatchDto(Author source);

    [MapperIgnoreTarget(nameof(Author.Id))]
    [MapperIgnoreTarget(nameof(Author.Books))]
    public partial void UpdateEntity(PatchAuthorDto source, Author target);
}

