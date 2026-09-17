using BookStore.Models;

namespace BookStore.BusinessLogic;

public interface IBookService
{
    Task<BookResponse> GetAsync(int id);
    Task<PagedResult<BookResponse>> SearchAsync(BookSearchQuery query);
    Task<BookResponse> CreateAsync(SaveBookRequest request);
    Task<BookResponse> UpdateAsync(int id, SaveBookRequest request);
    Task DeleteAsync(int id);
}
