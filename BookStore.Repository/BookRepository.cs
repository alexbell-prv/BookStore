using BookStore.Infrastructure;
using BookStore.Models;
using BookStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Repository;

public sealed class BookRepository : IBookRepository
{
    private readonly BookStoreDbContext db;
    private readonly IEfQueryExecutor queryExecutor;

    public BookRepository(BookStoreDbContext db, IEfQueryExecutor queryExecutor)
    {
        this.db = db;
        this.queryExecutor = queryExecutor;
    }

    public Task<Book?> GetAsync(int id)
    {
        var query = db.Books.Include(book => book.Author);
        return queryExecutor.FirstOrDefaultAsync(query, book => book.BookId == id);
    }

    public async Task<PagedResult<BookResponse>> SearchAsync(BookSearchQuery query)
    {
        var books = db.Books.AsNoTracking();
        if (query.Title is not null)
        {
            books = books.Where(book => book.Title.Contains(query.Title));
        }
        if (query.AuthorName is not null)
        {
            books = books.Where(book => book.Author.Name.Contains(query.AuthorName));
        }
        if (query.AuthorId is not null)
        {
            books = books.Where(book => book.AuthorId == query.AuthorId);
        }

        var total = await queryExecutor.CountAsync(books);
        var resultQuery = books
            .OrderBy(book => book.Title)
            .ThenBy(book => book.BookId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(book => new BookResponse(
                book.BookId,
                new AuthorResponse(book.AuthorId, book.Author.Name),
                book.Title,
                book.SubTitle));
        var items = await queryExecutor.ToListAsync(resultQuery);

        return new PagedResult<BookResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task AddAsync(Book book)
    {
        db.Books.Add(book);
        await DbStore.SaveAsync(db, queryExecutor);
    }

    public Task SaveAsync()
    {
        return DbStore.SaveAsync(db, queryExecutor);
    }

    public async Task DeleteAsync(Book book)
    {
        db.Books.Remove(book);
        await DbStore.SaveAsync(db, queryExecutor);
    }
}
