using BookStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure;

public sealed class BookStoreDbContext : DbContext
{
    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Author> Authors
    {
        get
        {
            return Set<Author>();
        }
    }

    public DbSet<Book> Books
    {
        get
        {
            return Set<Book>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC");
        modelBuilder.Entity<Author>(ConfigureAuthor);
        modelBuilder.Entity<Book>(ConfigureBook);
    }

    private static void ConfigureAuthor(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Author> entity)
    {
        entity.HasKey(author => author.AuthorId);
        entity.Property(author => author.Name).IsRequired().HasMaxLength(100);
        entity.HasIndex(author => new { author.Name, author.AuthorId });
        entity.ToTable("Authors", ConfigureAuthorTable);
    }

    private static void ConfigureAuthorTable(Microsoft.EntityFrameworkCore.Metadata.Builders.TableBuilder<Author> table)
    {
        table.HasCheckConstraint("CK_Authors_NameLength", "LEN([Name]) >= 3");
    }

    private static void ConfigureBook(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Book> entity)
    {
        entity.HasKey(book => book.BookId);
        entity.Property(book => book.Title).IsRequired().HasMaxLength(100);
        entity.HasIndex(book => new { book.Title, book.BookId });
        entity.HasOne(book => book.Author)
            .WithMany(author => author.Books)
            .HasForeignKey(book => book.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("Books", ConfigureBookTable);
    }

    private static void ConfigureBookTable(Microsoft.EntityFrameworkCore.Metadata.Builders.TableBuilder<Book> table)
    {
        table.HasCheckConstraint("CK_Books_TitleLength", "LEN([Title]) >= 3");
    }
}
