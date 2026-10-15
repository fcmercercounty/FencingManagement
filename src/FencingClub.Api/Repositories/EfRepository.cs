using System.Linq.Expressions;
using FencingClub.Api.Data;
using FencingClub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FencingClub.Api.Repositories;

public class EfRepository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    public const int MaxPageSize = 100;

    private DbSet<T> Set => db.Set<T>();

    public async Task<PagedResult<T>> ListAsync(Expression<Func<T, bool>>? filter, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        IQueryable<T> query = Set.AsNoTracking();
        if (filter is not null)
            query = query.Where(filter);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }

    public Task<T?> GetAsync(int id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        Set.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        Set.Update(entity);
        return db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        Set.Remove(entity);
        return db.SaveChangesAsync(ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        Set.AnyAsync(e => e.Id == id, ct);
}
