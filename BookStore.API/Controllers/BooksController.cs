using BookStore.BusinessLogic;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/books")]
[Authorize(Policy = Security.ManageBooks)]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService books;

    public BooksController(IBookService books)
    {
        this.books = books;
    }

    [HttpGet]
    public Task<PagedResult<BookResponse>> List([FromQuery] PageQuery query)
    {
        var searchQuery = new BookSearchQuery
        {
            Page = query.Page,
            PageSize = query.PageSize
        };
        return books.SearchAsync(searchQuery);
    }

    [HttpGet("{bookId:int}")]
    public Task<BookResponse> Get(int bookId)
    {
        return books.GetAsync(bookId);
    }

    [HttpPost]
    [ProducesResponseType<BookResponse>(201)]
    public async Task<ActionResult<BookResponse>> Create(SaveBookRequest request)
    {
        var book = await books.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { bookId = book.BookId }, book);
    }

    [HttpPut("{bookId:int}")]
    public Task<BookResponse> Update(int bookId, SaveBookRequest request)
    {
        return books.UpdateAsync(bookId, request);
    }

    [HttpDelete("{bookId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int bookId)
    {
        await books.DeleteAsync(bookId);
        return NoContent();
    }
}
