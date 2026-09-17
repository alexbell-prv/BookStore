using BookStore.Models;
using BookStore.Models.Entities;

namespace BookStore.Repository;

public interface IBookRepository
{
    Task<Book?> GetAsync(int id);
    Task<PagedResult<BookResponse>> SearchAsync(BookSearchQuery query);
    Task AddAsync(Book book);
    Task SaveAsync();
    Task DeleteAsync(Book book);
}
