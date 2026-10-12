using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Entities;
using LibraryManagement.Api.Errors;
using LibraryManagement.Api.Extensions;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryManagement.Api.Endpoints;

public static class MembersEndpoints
{
    public static void MapMemberEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/members")
            .WithTags("Members");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetMemberById")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Roles.Librarian);

        group.MapGet("/", GetAll)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(Roles.Librarian);

        group.MapGet("/{id:int}/loans", GetLoans)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPut("/{id:int}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateMemberDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Roles.Librarian);

        group.MapDelete("/{id:int}", Delete)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(Roles.Librarian);

        group.MapPatch("/{id:int}/restore", Restore)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(Roles.Librarian);
    }

    private static async Task<Results<Ok<ReadMemberDto>, ProblemHttpResult>> GetById(
        int id,
        LibraryDbContext dbContext,
        MemberMapper mapper)
    {
        var member = await dbContext.Members.FindAsync(id);

        if (member is null)
        {
            return MemberErrors.NotFound(id);
        }

        return TypedResults.Ok(mapper.ToReadDto(member));
    }

    private static async Task<Ok<List<ReadMemberDto>>> GetAll(
        LibraryDbContext dbContext,
        HttpContext httpContext,
        MemberMapper mapper,
        int pageNumber = PaginationParameters.DefaultPageNumber,
        int pageSize = PaginationParameters.DefaultPageSize)
    {
        var pagination = new PaginationParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var query = dbContext
            .Members
            .OrderBy(member => member.Id);

        var pagedResult = await query.ToPagedResult(
            pagination,
            member => mapper.ToReadDto(member));

        httpContext.Response.AppendPaginationHeaders(pagedResult);

        return TypedResults.Ok(pagedResult.Items);
    }

    private static async Task<Results<Ok<List<ReadLoanDto>>, ProblemHttpResult>> GetLoans(
        int id,
        LibraryDbContext dbContext,
        LoanMapper mapper,
        ClaimsPrincipal claimsPrincipal)
    {
        var member = await dbContext.Members.FindAsync(id);

        if (member is null)
        {
            return MemberErrors.NotFound(id);
        }

        var currentUserId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLibrarian = claimsPrincipal.IsInRole(Roles.Librarian);

        if(!isLibrarian && member.UserId != currentUserId)
        {
            return MemberErrors.Forbidden();
        }

        var loans = await dbContext.Loans
            .Include(loan => loan.Book)
            .Include(loan => loan.Member)
            .Where(loan => loan.MemberId == id)
            .OrderByDescending(loan => loan.LoanDate)
            .ThenByDescending(loan => loan.Id)
            .ToListAsync();

        var loanDtos = loans.Select(loan => mapper.ToReadDto(loan)).ToList();

        return TypedResults.Ok(loanDtos);
    }

    private static async Task<Results<Ok<ReadMemberDto>, ProblemHttpResult>> Update(
        int id,
        UpdateMemberDto updateDto,
        LibraryDbContext dbContext,
        MemberMapper mapper)
    {
        var existingMember = await dbContext.Members.FindAsync(id);

        if (existingMember is null)
        {
            return MemberErrors.NotFound(id);
        }

        mapper.UpdateEntity(updateDto, existingMember);

        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(mapper.ToReadDto(existingMember));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        LibraryDbContext dbContext)
    {
        var hasActiveLoans = await dbContext.Loans.AnyAsync(
            loan => loan.MemberId == id &&
                    loan.ReturnDate == null);  

        if(hasActiveLoans)
        {
            return MemberErrors.HasActiveLoans(id);
        }

        return await SetMemberActiveStatus(id, false, dbContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Restore(
        int id,
        LibraryDbContext dbContext)
    {
        return await SetMemberActiveStatus(id, true, dbContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SetMemberActiveStatus(
        int id,
        bool isActive,
        LibraryDbContext dbContext)
    {
        var member = await dbContext.Members.FindAsync(id);

        if (member is null)
        {
            return MemberErrors.NotFound(id);
        }

        member.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
