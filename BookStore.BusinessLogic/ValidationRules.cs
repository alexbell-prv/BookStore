using BookStore.Models;

namespace BookStore.BusinessLogic;

internal static class ValidationRules
{
    public static string RequiredText(string? value, string field)
    {
        var normalized = value?.Trim();
        if (normalized is null || normalized.Length is < 3 or > 100)
        {
            throw new BookStoreException(FailureKind.Validation, $"{field} must contain between 3 and 100 characters.");
        }
        return normalized;
    }

    public static void Page(PageQuery query)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
        {
            throw new BookStoreException(FailureKind.Validation, "Page must be positive, pageSize must be 1–100, and the page offset must fit in a 32-bit integer.");
        }
    }

    public static string? Filter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
