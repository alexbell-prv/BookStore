namespace BookStore.Models;

public static class Security
{
    public const string Audience = "bookstore.api";
    public const string ManageBooks = "books.manage";
    public const string ManageAuthors = "authors.manage";
    public const string SearchBooks = "books.search";
    public const string SearchRole = "bookstore.search";
    public const string DevelopmentSearchUsername = "bookstore.swagger.search";
}

public sealed class OAuthSettings
{
    public string Issuer { get; set; } = "http://localhost:5094";
    public string? MetadataAddress { get; set; }
    public string ManagementClientId { get; set; } = "bookstore.management";
    public string SearchClientId { get; set; } = "bookstore.swagger";
    public string ManagementClientSecret { get; set; } = string.Empty;
    public string SwaggerRedirectUri { get; set; } = "http://localhost:5093/swagger/oauth2-redirect.html";
}
