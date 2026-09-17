using BookStore.Models;

namespace BookStore.BusinessLogic;

public interface IAuthorService
{
    Task<AuthorResponse> GetAsync(int id);
    Task<PagedResult<AuthorResponse>> ListAsync(PageQuery query);
    Task<AuthorResponse> CreateAsync(SaveAuthorRequest request);
    Task<AuthorResponse> UpdateAsync(int id, SaveAuthorRequest request);
    Task DeleteAsync(int id);
}
