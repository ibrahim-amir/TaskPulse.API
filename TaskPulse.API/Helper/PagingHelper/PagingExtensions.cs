using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.Helper.PagingHelper
{
    // Turns any IQueryable into a sorted, counted page. Repository-side, after
    // the entity-specific Where clauses have been applied:
    //
    //     return await users
    //         .ApplySorting(query.SortBy, query.SortOrder)
    //         .ToPagedResultAsync(query);
    //
    // Nothing here knows about User or any other entity, so it works for
    // Projects, Tasks and everything after them without changes.
    public static class PagingExtensions
    {
        // Sorts by property name at runtime, the same reflection approach the
        // existence checker uses. An unknown property name is ignored instead of
        // throwing — a bad ?sortBy= in a URL shouldn't fail the whole request.
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query;

            var property = typeof(T).GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
                return query;

            var parameter = Expression.Parameter(typeof(T), "e");
            var propertyAccess = Expression.Property(parameter, property);
            var keySelector = Expression.Lambda(propertyAccess, parameter);

            var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

            var orderByCall = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(keySelector));

            return query.Provider.CreateQuery<T>(orderByCall);
        }

        // Two queries: one COUNT for the total, one for the page itself.
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PagedRequest request)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return PagedResult<T>.Create(items, totalCount, request);
        }
    }
}
