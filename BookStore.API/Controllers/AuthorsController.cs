using BookStore.BusinessLogic;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/authors")]
[Authorize(Policy = Security.ManageAuthors)]
public sealed class AuthorsController : ControllerBase
{
    private readonly IAuthorService authors;

    public AuthorsController(IAuthorService authors)
    {
        this.authors = authors;
    }

    [HttpGet]
    public Task<PagedResult<AuthorResponse>> List([FromQuery] PageQuery query)
    {
        return authors.ListAsync(query);
    }

    [HttpGet("{authorId:int}")]
    public Task<AuthorResponse> Get(int authorId)
    {
        return authors.GetAsync(authorId);
    }

    [HttpPost]
    [ProducesResponseType<AuthorResponse>(201)]
    public async Task<ActionResult<AuthorResponse>> Create(SaveAuthorRequest request)
    {
        var author = await authors.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { authorId = author.AuthorId }, author);
    }

    [HttpPut("{authorId:int}")]
    public Task<AuthorResponse> Update(int authorId, SaveAuthorRequest request)
    {
        return authors.UpdateAsync(authorId, request);
    }

    [HttpDelete("{authorId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int authorId)
    {
        await authors.DeleteAsync(authorId);
        return NoContent();
    }
}
