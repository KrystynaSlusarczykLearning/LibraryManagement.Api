using LibraryManagement.Api.Common;
using LibraryManagement.Api.DataAccess;
using LibraryManagement.Api.DTOs.Members;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Mapping;
using LibraryManagement.Api.RequestParameters;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAll);

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateMemberDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateMemberDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/restore", Restore)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<ReadMemberDto>, ProblemHttpResult>> GetById(
        int id,
        LibraryDbContext dbContext,
        MemberMapper mapper)
    {
        var member = await dbContext.Members.FindAsync(id);

        if (member is null)
        {
            return TypedResults.Problem(
                detail: $"A member with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
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

        var query = dbContext.Members.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);

        var members = await query
            .OrderBy(member => member.Id)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var readDtos = members
            .Select(mapper.ToReadDto)
            .ToList();

        httpContext.Response.Headers.Append(ResponseHeaders.TotalCount, totalCount.ToString());
        httpContext.Response.Headers.Append(ResponseHeaders.TotalPages, totalPages.ToString());

        return TypedResults.Ok(readDtos);
    }

    private static async Task<CreatedAtRoute<ReadMemberDto>> Create(
        CreateMemberDto createDto,
        LibraryDbContext dbContext,
        MemberMapper mapper)
    {
        var member = mapper.ToEntity(createDto);
        member.IsActive = true;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        return TypedResults.CreatedAtRoute(
            mapper.ToReadDto(member),
            "GetMemberById",
            new { id = member.Id });
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
            return TypedResults.Problem(
                detail: $"A member with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        mapper.UpdateEntity(updateDto, existingMember);

        await dbContext.SaveChangesAsync();

        return TypedResults.Ok(mapper.ToReadDto(existingMember));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id,
        LibraryDbContext dbContext)
    {
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
            return TypedResults.Problem(
                detail: $"A member with Id {id} has not been found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        member.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
