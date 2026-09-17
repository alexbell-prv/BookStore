using System.Security.Claims;
using BookStore.Host;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class AuthorizationTests
{
    private const string ManagementClient = "test.management";
    private const string SearchClient = "test.search";
    private ServiceProvider services = null!;
    private IAuthorizationService authorization = null!;

    [SetUp]
    public void SetUp()
    {
        services = new ServiceCollection()
            .AddLogging()
            .AddBookStoreAuthentication(new OAuthSettings
            {
                ManagementClientId = ManagementClient,
                SearchClientId = SearchClient
            }, development: true)
            .BuildServiceProvider();
        authorization = services.GetRequiredService<IAuthorizationService>();
    }

    [TearDown]
    public void TearDown()
    {
        services.Dispose();
    }

    [TestCase(SearchClient, "reader", Security.SearchBooks, true)]
    [TestCase(SearchClient, "reader", "other books.search", true)]
    [TestCase(SearchClient, null, Security.SearchBooks, false)]
    [TestCase(ManagementClient, null, Security.SearchBooks, false)]
    [TestCase(ManagementClient, "reader", Security.SearchBooks, false)]
    [TestCase("other.client", "reader", Security.SearchBooks, false)]
    [TestCase(null, "reader", Security.SearchBooks, false)]
    [TestCase(SearchClient, "reader", null, false)]
    [TestCase(SearchClient, "reader", Security.ManageBooks, false)]
    [TestCase(SearchClient, "reader", "books.search.extra", false)]
    public async Task Search_RequiresSearchClientUserAndScope(string? clientId, string? subject, string? scope, bool allowed)
    {
        var user = CreateUser(clientId, subject, scope);

        var result = await authorization.AuthorizeAsync(user, null, Security.SearchBooks);

        Assert.That(result.Succeeded, Is.EqualTo(allowed));
    }

    [TestCase(ManagementClient, null, true)]
    [TestCase(ManagementClient, "reader", false)]
    [TestCase(SearchClient, "reader", false)]
    [TestCase(SearchClient, null, false)]
    [TestCase("other.client", null, false)]
    [TestCase(null, null, false)]
    public async Task Management_RequiresClientCredentials(string? clientId, string? subject, bool allowed)
    {
        foreach (var scope in new[] { Security.ManageBooks, Security.ManageAuthors })
        {
            var user = CreateUser(clientId, subject, scope);

            var result = await authorization.AuthorizeAsync(user, null, scope);

            Assert.That(result.Succeeded, Is.EqualTo(allowed), scope);
        }
    }

    [TestCase(Security.ManageBooks, Security.ManageAuthors)]
    [TestCase(Security.ManageAuthors, Security.ManageBooks)]
    [TestCase(Security.ManageBooks, null)]
    [TestCase(Security.ManageAuthors, null)]
    public async Task Management_RequiresMatchingScope(string policy, string? scope)
    {
        var user = CreateUser(ManagementClient, null, scope);

        var result = await authorization.AuthorizeAsync(user, null, policy);

        Assert.That(result.Succeeded, Is.False);
    }

    [TestCase(Security.ManageBooks, ManagementClient, null)]
    [TestCase(Security.ManageAuthors, ManagementClient, null)]
    [TestCase(Security.SearchBooks, SearchClient, "reader")]
    public async Task Policies_RejectUnauthenticatedIdentity(string policy, string clientId, string? subject)
    {
        var claims = CreateUser(clientId, subject, policy).Claims;
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = await authorization.AuthorizeAsync(user, null, policy);

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task Search_AcceptsSeparateScopeClaims()
    {
        var user = CreateUser(SearchClient, "reader", "other");
        ((ClaimsIdentity)user.Identity!).AddClaim(new Claim("scope", Security.SearchBooks));

        var result = await authorization.AuthorizeAsync(user, null, Security.SearchBooks);

        Assert.That(result.Succeeded, Is.True);
    }

    private static ClaimsPrincipal CreateUser(string? clientId, string? subject, string? scope)
    {
        var identity = new ClaimsIdentity("Bearer");
        if (clientId is not null)
        {
            identity.AddClaim(new Claim("client_id", clientId));
        }
        if (subject is not null)
        {
            identity.AddClaim(new Claim("sub", subject));
        }
        if (scope is not null)
        {
            identity.AddClaim(new Claim("scope", scope));
        }
        return new ClaimsPrincipal(identity);
    }
}
