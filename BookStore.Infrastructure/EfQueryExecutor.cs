using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure;

public interface IEfQueryExecutor
{
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate);
    Task<int> CountAsync<T>(IQueryable<T> query);
    Task<List<T>> ToListAsync<T>(IQueryable<T> query);
    Task<int> SaveChangesAsync(DbContext dbContext);
}

public sealed class EfQueryExecutor : IEfQueryExecutor
{
    private readonly RequestCancellationContext cancellationContext;

    public EfQueryExecutor(RequestCancellationContext cancellationContext)
    {
        this.cancellationContext = cancellationContext;
    }

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        return query.FirstOrDefaultAsync(predicate, cancellationContext.CancellationToken);
    }

    public Task<bool> AnyAsync<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        return query.AnyAsync(predicate, cancellationContext.CancellationToken);
    }

    public Task<int> CountAsync<T>(IQueryable<T> query)
    {
        return query.CountAsync(cancellationContext.CancellationToken);
    }

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query)
    {
        return query.ToListAsync(cancellationContext.CancellationToken);
    }

    public Task<int> SaveChangesAsync(DbContext dbContext)
    {
        return dbContext.SaveChangesAsync(cancellationContext.CancellationToken);
    }
}
