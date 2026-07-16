using LibraryManagement.Api.DTOs.Authors;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper]
public partial class AuthorMapper
{
    public partial ReadAuthorDto ToReadDto(Author source);

    [MapperIgnoreTarget(nameof(Author.Id))]
    public partial Author ToEntity(CreateAuthorDto source);

    [MapperIgnoreTarget(nameof(Author.Id))]
    public partial void UpdateEntity(UpdateAuthorDto source, Author target);
}

