using BookStore.IdentityServer;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class SecurityHeadersMiddlewareTests
{
    [TestCase("http://localhost:5093/swagger/oauth2-redirect.html", "http://localhost:5093")]
    [TestCase("https://books.example.test/swagger/oauth2-redirect.html", "https://books.example.test")]
    public async Task Login_AllowsConfiguredSwaggerCallbackWithoutTrustingReturnUrl(string redirectUri, string origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/Account/Login";
        context.Request.QueryString = new QueryString("?ReturnUrl=https%3A%2F%2Funtrusted.example");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, redirectUri);

        await middleware.InvokeAsync(context);

        var directives = context.Response.Headers.ContentSecurityPolicy.ToString().Split(';', StringSplitOptions.TrimEntries);
        Assert.Multiple(() =>
        {
            Assert.That(directives, Does.Contain($"form-action 'self' {origin}"));
            Assert.That(directives, Does.Contain("frame-ancestors 'none'"));
            Assert.That(directives, Does.Contain("default-src 'self'"));
            Assert.That(context.Response.Headers.CacheControl.ToString(), Does.Contain("no-store"));
        });
    }
}
