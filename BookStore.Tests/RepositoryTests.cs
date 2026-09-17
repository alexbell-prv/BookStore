using BookStore.Infrastructure;
using BookStore.Models.Entities;
using BookStore.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class RepositoryTests
{
    [Test]
    public void AddBook_WhenSaveFails_PreservesOriginalException()
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlServer("Server=localhost;Database=BookStore.Tests;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        using var db = new BookStoreDbContext(options);
        var queryExecutor = new Mock<IEfQueryExecutor>();
        var expectedException = new InvalidOperationException("Database write failed.");
        queryExecutor
            .Setup(executor => executor.SaveChangesAsync(db))
            .ThrowsAsync(expectedException);
        var repository = new BookRepository(db, queryExecutor.Object);
        var book = new Book
        {
            AuthorId = 7,
            Author = new Author { AuthorId = 7, Name = "Ursula Le Guin" },
            Title = "The Dispossessed"
        };

        var actualException = Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(book));

        Assert.That(actualException, Is.SameAs(expectedException));
    }
}
