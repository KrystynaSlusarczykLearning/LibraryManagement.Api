using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace LibraryManagement.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class LoanMapper
{
    public partial ReadLoanDto ToReadDto(Loan source);

    [MapperIgnoreTarget(nameof(Loan.Id))]
    [MapperIgnoreTarget(nameof(Loan.Book))]
    [MapperIgnoreTarget(nameof(Loan.Member))]
    [MapperIgnoreTarget(nameof(Loan.ReturnDate))]
    public partial Loan ToEntity(CreateLoanDto source);
}