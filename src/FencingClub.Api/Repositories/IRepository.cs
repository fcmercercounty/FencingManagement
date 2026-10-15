using System.Linq.Expressions;
using FencingClub.Api.Models;

namespace FencingClub.Api.Repositories;

/// <summary>Generic CRUD over any <see cref="BaseEntity"/>, backed by Entity Framework Core.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<PagedResult<T>> ListAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken ct = default);
    Task<T?> GetAsync(int id, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
