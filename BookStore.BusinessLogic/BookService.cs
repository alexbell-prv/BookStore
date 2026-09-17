using BookStore.Models;
using BookStore.Models.Entities;
using BookStore.Repository;

namespace BookStore.BusinessLogic;

public sealed class BookService : IBookService
{
    private readonly IBookRepository books;
    private readonly IAuthorRepository authors;

    public BookService(IBookRepository books, IAuthorRepository authors)
    {
        this.books = books;
        this.authors = authors;
    }

    public async Task<BookResponse> GetAsync(int id)
    {
        var book = await FindAsync(id);
        return Map(book);
    }

    public Task<PagedResult<BookResponse>> SearchAsync(BookSearchQuery query)
    {
        ValidationRules.Page(query);
        if (query.AuthorId is <= 0)
        {
            throw new BookStoreException(FailureKind.Validation, "AuthorId must be positive.");
        }

        var normalizedQuery = new BookSearchQuery
        {
            Page = query.Page,
            PageSize = query.PageSize,
            AuthorId = query.AuthorId,
            Title = ValidationRules.Filter(query.Title),
            AuthorName = ValidationRules.Filter(query.AuthorName)
        };
        return books.SearchAsync(normalizedQuery);
    }

    public async Task<BookResponse> CreateAsync(SaveBookRequest request)
    {
        var title = ValidationRules.RequiredText(request.Title, "Title");
        var author = await FindAuthorAsync(request.AuthorId);
        var book = new Book
        {
            Title = title,
            SubTitle = request.SubTitle,
            AuthorId = author.AuthorId,
            Author = author
        };
        await books.AddAsync(book);
        return Map(book);
    }

    public async Task<BookResponse> UpdateAsync(int id, SaveBookRequest request)
    {
        var book = await FindAsync(id);
        var title = ValidationRules.RequiredText(request.Title, "Title");
        var author = await FindAuthorAsync(request.AuthorId);
        book.Title = title;
        book.SubTitle = request.SubTitle;
        book.AuthorId = author.AuthorId;
        book.Author = author;
        await books.SaveAsync();
        return Map(book);
    }

    public async Task DeleteAsync(int id)
    {
        var book = await FindAsync(id);
        await books.DeleteAsync(book);
    }

    private async Task<Book> FindAsync(int id)
    {
        return await books.GetAsync(id)
            ?? throw new BookStoreException(FailureKind.NotFound, $"Book {id} was not found.");
    }

    private async Task<Author> FindAuthorAsync(int id)
    {
        if (id <= 0)
        {
            throw new BookStoreException(FailureKind.Validation, "AuthorId must be positive.");
        }

        return await authors.GetAsync(id)
            ?? throw new BookStoreException(FailureKind.Validation, $"Author {id} does not exist.");
    }

    private static BookResponse Map(Book book)
    {
        var author = new AuthorResponse(book.AuthorId, book.Author.Name);
        return new BookResponse(book.BookId, author, book.Title, book.SubTitle);
    }
}
