using BookStore.Models;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Identity;

public sealed class IdentityConfigurationSeeder
{
    private readonly IdentityConfigurationDbContext db;

    public IdentityConfigurationSeeder(IdentityConfigurationDbContext db)
    {
        this.db = db;
    }

    public async Task SeedAsync(OAuthSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings.ManagementClientSecret.Length < 32)
        {
            throw new InvalidOperationException("Set OAuth:ManagementClientSecret to a random secret of at least 32 characters before initializing identity.");
        }
        var origin = new Uri(settings.SwaggerRedirectUri).GetLeftPart(UriPartial.Authority);
        await db.Database.CreateExecutionStrategy().ExecuteAsync(SeedDatabaseAsync);

        async Task SeedDatabaseAsync()
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            foreach (var name in new[] { Security.ManageBooks, Security.ManageAuthors, Security.SearchBooks })
            {
                if (!await db.ApiScopes.AnyAsync(s => s.Name == name, cancellationToken))
                {
                    db.ApiScopes.Add(new ApiScope(name).ToEntity());
                }
            }

            // Explicit initialization synchronizes only the two application-owned clients and API resource.
            await db.Clients.Where(c => c.ClientId == settings.ManagementClientId || c.ClientId == settings.SearchClientId)
                .ExecuteDeleteAsync(cancellationToken);
            await db.ApiResources.Where(r => r.Name == Security.Audience).ExecuteDeleteAsync(cancellationToken);
            db.ApiResources.Add(new ApiResource(Security.Audience, "Bookstore API")
            {
                Scopes = { Security.ManageBooks, Security.ManageAuthors, Security.SearchBooks }
            }.ToEntity());
            db.Clients.Add(new Client
            {
                ClientId = settings.ManagementClientId,
                ClientName = "Bookstore management",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret(settings.ManagementClientSecret.Sha256()) },
                AllowedScopes = { Security.ManageBooks, Security.ManageAuthors },
                AllowedCorsOrigins = { origin },
                AccessTokenLifetime = 300
            }.ToEntity());
            db.Clients.Add(new Client
            {
                ClientId = settings.SearchClientId,
                ClientName = "Bookstore Swagger search",
                AllowedGrantTypes = GrantTypes.Implicit,
                RequireClientSecret = false,
                RequireConsent = false,
                AllowAccessTokensViaBrowser = true,
                AllowedScopes = { Security.SearchBooks },
                RedirectUris = { settings.SwaggerRedirectUri },
                AllowedCorsOrigins = { origin },
                AccessTokenLifetime = 300,
                AllowOfflineAccess = false
            }.ToEntity());
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
