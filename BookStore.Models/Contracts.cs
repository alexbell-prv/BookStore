namespace BookStore.Models;

public sealed record AuthorResponse(int AuthorId, string Name);
public sealed record BookResponse(int BookId, AuthorResponse Author, string Title, string? SubTitle);
public sealed record SaveAuthorRequest(string? Name);
public sealed record SaveBookRequest(int AuthorId, string? Title, string? SubTitle = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages
    {
        get
        {
            return (int)Math.Ceiling((double)TotalCount / PageSize);
        }
    }
}

public class PageQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class BookSearchQuery : PageQuery
{
    public string? Title { get; set; }
    public string? AuthorName { get; set; }
    public int? AuthorId { get; set; }
}
