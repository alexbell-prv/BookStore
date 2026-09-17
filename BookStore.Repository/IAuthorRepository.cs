using BookStore.Models;
using BookStore.Models.Entities;

namespace BookStore.Repository;

public interface IAuthorRepository
{
    Task<Author?> GetAsync(int id);
    Task<PagedResult<AuthorResponse>> ListAsync(PageQuery query);
    Task<bool> HasBooksAsync(int id);
    Task AddAsync(Author author);
    Task SaveAsync();
    Task DeleteAsync(Author author);
}
