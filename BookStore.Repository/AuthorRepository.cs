using BookStore.Infrastructure;
using BookStore.Models;
using BookStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repository;

public sealed class AuthorRepository : IAuthorRepository
{
    private readonly BookStoreDbContext db;
    private readonly IEfQueryExecutor queryExecutor;

    public AuthorRepository(BookStoreDbContext db, IEfQueryExecutor queryExecutor)
    {
        this.db = db;
        this.queryExecutor = queryExecutor;
    }

    public Task<Author?> GetAsync(int id)
    {
        return queryExecutor.FirstOrDefaultAsync(db.Authors, author => author.AuthorId == id);
    }

    public Task<bool> HasBooksAsync(int id)
    {
        return queryExecutor.AnyAsync(db.Books, book => book.AuthorId == id);
    }

    public async Task<PagedResult<AuthorResponse>> ListAsync(PageQuery query)
    {
        var authors = db.Authors.AsNoTracking();
        var total = await queryExecutor.CountAsync(authors);
        var resultQuery = authors
            .OrderBy(author => author.Name)
            .ThenBy(author => author.AuthorId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(author => new AuthorResponse(author.AuthorId, author.Name));
        var items = await queryExecutor.ToListAsync(resultQuery);

        return new PagedResult<AuthorResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task AddAsync(Author author)
    {
        db.Authors.Add(author);
        await DbStore.SaveAsync(db, queryExecutor);
    }

    public Task SaveAsync()
    {
        return DbStore.SaveAsync(db, queryExecutor);
    }

    public async Task DeleteAsync(Author author)
    {
        db.Authors.Remove(author);
        await DbStore.SaveAsync(db, queryExecutor, deletingAuthor: true);
    }
}
