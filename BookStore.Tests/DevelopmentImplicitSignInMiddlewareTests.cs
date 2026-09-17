using BookStore.IdentityServer;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class DevelopmentImplicitSignInMiddlewareTests
{
    [TestCase("/connect/authorize", "bookstore.swagger", "token", "books.search", true)]
    [TestCase("/connect/authorize", "bookstore.swagger", "token", "openid books.search", true)]
    [TestCase("/connect/authorize", "bookstore.swagger", "code", "books.search", false)]
    [TestCase("/connect/authorize", "bookstore.management", "token", "books.search", false)]
    [TestCase("/connect/authorize", "bookstore.swagger", "token", "books.manage", false)]
    [TestCase("/Account/Login", "bookstore.swagger", "token", "books.search", false)]
    public void IsSearchAuthorizationRequest_OnlyAcceptsSwaggerImplicitSearch(
        string path,
        string clientId,
        string responseType,
        string scope,
        bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.QueryString = QueryString.Create(new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["response_type"] = responseType,
            ["scope"] = scope
        });

        var result = DevelopmentImplicitSignInMiddleware.IsSearchAuthorizationRequest(
            context.Request,
            "bookstore.swagger");

        Assert.That(result, Is.EqualTo(expected));
    }
}
