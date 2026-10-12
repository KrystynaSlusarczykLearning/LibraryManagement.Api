using LibraryManagement.Api.CustomResults;
using LibraryManagement.Api.RequestParameters;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<TReadDto>> ToPagedResult<TEntity, TReadDto>(
        this IOrderedQueryable<TEntity> query,
        PaginationParameters pagination,
        Func<TEntity, TReadDto> mappingFunction)
    {
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var readDtos = items
            .Select(mappingFunction)
            .ToList();

        return new PagedResult<TReadDto>(
            readDtos,
            totalCount,
            pagination);
    }
}
