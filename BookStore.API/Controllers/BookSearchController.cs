using BookStore.BusinessLogic;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/books/search")]
[Authorize(Policy = Security.SearchBooks)]
public sealed class BookSearchController : ControllerBase
{
    private readonly IBookService books;

    public BookSearchController(IBookService books)
    {
        this.books = books;
    }

    [HttpGet]
    public Task<PagedResult<BookResponse>> Search([FromQuery] BookSearchQuery query)
    {
        return books.SearchAsync(query);
    }
}
