using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class LoansService(
    LibraryDbContext dbContext,
    LoanMapper mapper) : ILoansService
{
    public async Task<ErrorOr<ReadLoanDto>> GetByIdAsync(int id)
    {
        var loan = await dbContext.Loans
            .Include(loan => loan.Member)
            .Include(loan => loan.Book)
            .FirstOrDefaultAsync(loan => loan.Id == id);

        if (loan is null)
        {
            return LoanErrors.LoanNotFound(id);
        }

        return mapper.ToReadDto(loan);
    }

    public async Task<ErrorOr<PagedResult<ReadLoanDto>>> GetAllAsync(
        PaginationParameters pagination,
        LoanFilteringParameters filteringParameters,
        SortingParameters<LoanSortBy> sortingParameters)
    {
        return await dbContext.Loans
            .Include(loan => loan.Member)
            .Include(loan => loan.Book)
            .ApplyFiltering(filteringParameters)
            .ApplySorting(sortingParameters)
            .ToPagedResult(
                pagination,
                loan => mapper.ToReadDto(loan));
    }

    public async Task<ErrorOr<ReadLoanDto>> CreateAsync(CreateLoanDto createDto)
    {
        if(createDto.LoanDate > createDto.DueDate)
        {
            return LoanErrors.LoanDateAfterDueDate();
        }

        var book = await dbContext.Books.FirstOrDefaultAsync(
            book => book.Id == createDto.BookId);

        if (book is null)
        {
            return LoanErrors.BookNotFound(createDto.BookId);
        }

        if(book.AvailableCopies == 0)
        {
            return LoanErrors.NoAvailableCopies(createDto.BookId);
        }

        var member = await dbContext.Members.FirstOrDefaultAsync(
           member => member.Id == createDto.MemberId);

        if (member is null)
        {
            return LoanErrors.MemberNotFound(createDto.MemberId);
        }

        if(!member.IsActive)
        {
            return LoanErrors.MemberInactive(createDto.MemberId);
        }

        var loan = mapper.ToEntity(createDto);
        book.AvailableCopies--;
        loan.Book = book;
        loan.Member = member;

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync();

        return mapper.ToReadDto(loan);
    }

    public async Task<ErrorOr<ReadLoanDto>> ReturnAsync(int id)
    {
        var loan = await dbContext.Loans
            .Include(loan => loan.Member)
            .Include(loan => loan.Book)
            .FirstOrDefaultAsync(loan => loan.Id == id);

        if (loan is null)
        {
            return LoanErrors.LoanNotFound(id);
        }

        if(loan.ReturnDate is not null)
        {
            return LoanErrors.AlreadyReturned(id);
        }

        loan.ReturnDate = DateOnly.FromDateTime(DateTime.Today);
        loan.Book!.AvailableCopies++;

        await dbContext.SaveChangesAsync();

        return mapper.ToReadDto(loan);
    }
}
