using BookStore.Models;
using BookStore.Models.Entities;
using BookStore.Repository;

namespace BookStore.BusinessLogic;

public sealed class AuthorService : IAuthorService
{
    private readonly IAuthorRepository authors;

    public AuthorService(IAuthorRepository authors)
    {
        this.authors = authors;
    }

    public async Task<AuthorResponse> GetAsync(int id)
    {
        var author = await FindAsync(id);
        return Map(author);
    }

    public Task<PagedResult<AuthorResponse>> ListAsync(PageQuery query)
    {
        ValidationRules.Page(query);
        return authors.ListAsync(query);
    }

    public async Task<AuthorResponse> CreateAsync(SaveAuthorRequest request)
    {
        var author = new Author
        {
            Name = ValidationRules.RequiredText(request.Name, "Name")
        };
        await authors.AddAsync(author);
        return Map(author);
    }

    public async Task<AuthorResponse> UpdateAsync(int id, SaveAuthorRequest request)
    {
        var author = await FindAsync(id);
        author.Name = ValidationRules.RequiredText(request.Name, "Name");
        await authors.SaveAsync();
        return Map(author);
    }

    public async Task DeleteAsync(int id)
    {
        var author = await FindAsync(id);
        if (await authors.HasBooksAsync(id))
        {
            throw new BookStoreException(FailureKind.Conflict, "An author with books cannot be deleted.");
        }
        await authors.DeleteAsync(author);
    }

    private async Task<Author> FindAsync(int id)
    {
        return await authors.GetAsync(id)
            ?? throw new BookStoreException(FailureKind.NotFound, $"Author {id} was not found.");
    }

    private static AuthorResponse Map(Author author)
    {
        return new AuthorResponse(author.AuthorId, author.Name);
    }
}
