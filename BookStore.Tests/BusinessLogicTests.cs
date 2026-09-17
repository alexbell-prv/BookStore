using BookStore.BusinessLogic;
using BookStore.Models;
using BookStore.Models.Entities;
using BookStore.Repository;
using Moq;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class BusinessLogicTests
{
    private Mock<IBookRepository> books = null!;
    private Mock<IAuthorRepository> authors = null!;
    private BookService bookService = null!;
    private AuthorService authorService = null!;

    [SetUp]
    public void SetUp()
    {
        books = new Mock<IBookRepository>();
        authors = new Mock<IAuthorRepository>();
        bookService = new BookService(books.Object, authors.Object);
        authorService = new AuthorService(authors.Object);
    }

    [Test]
    public async Task CreateBook_WithExistingAuthor_ReturnsSavedBook()
    {
        authors.Setup(repository => repository.GetAsync(7))
            .ReturnsAsync(new Author { AuthorId = 7, Name = "Ursula Le Guin" });
        books.Setup(repository => repository.AddAsync(It.IsAny<Book>()))
            .Callback<Book>(book => book.BookId = 42)
            .Returns(Task.CompletedTask);

        var result = await bookService.CreateAsync(new SaveBookRequest(7, "  The Dispossessed  "));

        Assert.Multiple(() =>
        {
            Assert.That(result.BookId, Is.EqualTo(42));
            Assert.That(result.Title, Is.EqualTo("The Dispossessed"));
            Assert.That(result.Author, Is.EqualTo(new AuthorResponse(7, "Ursula Le Guin")));
            Assert.That(result.SubTitle, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("ab")]
    [TestCase("  ab  ")]
    public void Create_WithInvalidText_IsRejected(string? value)
    {
        AssertValidation(() => bookService.CreateAsync(new SaveBookRequest(7, value)));
        AssertValidation(() => authorService.CreateAsync(new SaveAuthorRequest(value)));
    }

    [Test]
    public void Create_WithTextOver100Characters_IsRejected()
    {
        var value = new string('a', 101);
        AssertValidation(() => bookService.CreateAsync(new SaveBookRequest(7, value)));
        AssertValidation(() => authorService.CreateAsync(new SaveAuthorRequest(value)));
    }

    [Test]
    public void CreateBook_WithUnknownAuthor_IsRejected()
    {
        AssertValidation(() => bookService.CreateAsync(new SaveBookRequest(99, "A valid title")));
        books.Verify(repository => repository.AddAsync(It.IsAny<Book>()), Times.Never);
    }

    [Test]
    public void UpdateBook_WhenMissing_ReturnsNotFound()
    {
        var error = Assert.ThrowsAsync<BookStoreException>(
            () => bookService.UpdateAsync(99, new SaveBookRequest(7, "A valid title")));
        Assert.That(error!.Kind, Is.EqualTo(FailureKind.NotFound));
    }

    [Test]
    public void DeleteAuthor_WithBooks_IsRejected()
    {
        var author = new Author { AuthorId = 7, Name = "Ursula Le Guin" };
        authors.Setup(repository => repository.GetAsync(7)).ReturnsAsync(author);
        authors.Setup(repository => repository.HasBooksAsync(7)).ReturnsAsync(true);

        var error = Assert.ThrowsAsync<BookStoreException>(() => authorService.DeleteAsync(7));

        Assert.That(error!.Kind, Is.EqualTo(FailureKind.Conflict));
        authors.Verify(repository => repository.DeleteAsync(It.IsAny<Author>()), Times.Never);
    }

    [TestCase(0, 20)]
    [TestCase(-1, 20)]
    [TestCase(1, 0)]
    [TestCase(1, 101)]
    [TestCase(int.MaxValue, 100)]
    public void Search_WithInvalidPagination_IsRejected(int page, int pageSize)
    {
        AssertValidation(() => bookService.SearchAsync(new BookSearchQuery { Page = page, PageSize = pageSize }));
        AssertValidation(() => authorService.ListAsync(new PageQuery { Page = page, PageSize = pageSize }));
    }

    private static void AssertValidation(AsyncTestDelegate action)
    {
        var error = Assert.ThrowsAsync<BookStoreException>(action);
        Assert.That(error!.Kind, Is.EqualTo(FailureKind.Validation));
    }
}
