using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper]
public partial class MemberMapper
{
    [MapperIgnoreSource(nameof(Member.Loans))]
    public partial ReadMemberDto ToReadDto(Member source);

    [MapperIgnoreTarget(nameof(Member.Id))]
    [MapperIgnoreTarget(nameof(Member.IsActive))]
    [MapperIgnoreTarget(nameof(Member.Loans))]
    public partial Member ToEntity(CreateMemberDto source);

    [MapperIgnoreTarget(nameof(Member.Id))]
    [MapperIgnoreTarget(nameof(Member.IsActive))]
    [MapperIgnoreTarget(nameof(Member.Loans))]
    public partial void UpdateEntity(UpdateMemberDto source, Member target);
}
