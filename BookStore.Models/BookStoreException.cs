namespace BookStore.Models;

public enum FailureKind
{
    Validation,
    NotFound,
    Conflict
}

public sealed class BookStoreException : Exception
{
    public BookStoreException(FailureKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public BookStoreException(FailureKind kind, string message, Exception innerException)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public FailureKind Kind { get; }
}
