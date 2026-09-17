using BookStore.Infrastructure;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BookStore.Repository;

internal static class DbStore
{
    private const int ForeignKeyViolationNumber = 547;

    public static async Task SaveAsync(
        BookStoreDbContext db,
        IEfQueryExecutor queryExecutor,
        bool deletingAuthor = false)
    {
        try
        {
            await queryExecutor.SaveChangesAsync(db);
        }
        catch (DbUpdateException exception) when (deletingAuthor && IsForeignKeyViolation(exception))
        {
            throw new BookStoreException(
                FailureKind.Conflict,
                "An author with books cannot be deleted.",
                exception);
        }
    }

    private static bool IsForeignKeyViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number == ForeignKeyViolationNumber;
    }
}
