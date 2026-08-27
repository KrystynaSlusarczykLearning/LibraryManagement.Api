using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class BookMapper
{
    public partial ReadBookDto ToReadDto(Book source);

    [MapperIgnoreTarget(nameof(Book.Id))]
    [MapperIgnoreTarget(nameof(Book.AvailableCopies))]
    [MapperIgnoreTarget(nameof(Book.Category))]
    [MapperIgnoreTarget(nameof(Book.Authors))]
    public partial Book ToEntity(CreateBookDto source);

    [MapperIgnoreTarget(nameof(Book.Id))]
    [MapperIgnoreTarget(nameof(Book.AvailableCopies))]
    [MapperIgnoreTarget(nameof(Book.Category))]
    [MapperIgnoreTarget(nameof(Book.Authors))]
    public partial void UpdateEntity(UpdateBookDto source, Book target);
}

