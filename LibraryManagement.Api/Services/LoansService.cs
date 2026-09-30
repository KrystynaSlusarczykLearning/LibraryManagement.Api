using ErrorOr;
using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.Entities;
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
        var query = dbContext.Loans
            .Include(loan => loan.Member)
            .Include(loan => loan.Book)
            .AsQueryable();

        if (filteringParameters.BookId is not null)
        {
            query = query.Where(
                loan => loan.BookId == filteringParameters.BookId);
        }

        if (filteringParameters.MemberId is not null)
        {
            query = query.Where(
                loan => loan.MemberId == filteringParameters.MemberId);
        }

        if (filteringParameters.LoanStatus is not null)
        {
            query = filteringParameters.LoanStatus switch
            {
                LoanStatus.Returned =>
                    query.Where(loan => loan.ReturnDate != null),
                LoanStatus.Overdue =>
                    query.Where(loan => loan.ReturnDate == null &&
                                        loan.DueDate < DateOnly.FromDateTime(DateTime.Today)),
                LoanStatus.Active =>
                    query.Where(loan => loan.ReturnDate == null &&
                                        loan.DueDate >= DateOnly.FromDateTime(DateTime.Today))
            };
        }

        query = sortingParameters.SortBy switch
        {
            LoanSortBy.DueDate => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(loan => loan.DueDate) :
                query.OrderByDescending(loan => loan.DueDate),
            LoanSortBy.LoanDate => sortingParameters.SortDirection == SortDirection.Asc ?
                query.OrderBy(loan => loan.LoanDate) :
                query.OrderByDescending(loan => loan.LoanDate),
            _ => query.OrderBy(loan => loan.Id)
        };

        return await query.ToPagedResult(
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
