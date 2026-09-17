using System.Security.Claims;
using BookStore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace BookStore.Host;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddBookStoreAuthentication(this IServiceCollection services, OAuthSettings oauth, bool development)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(ConfigureJwtBearer);
        services.AddAuthorization(ConfigureAuthorization);
        return services;

        void ConfigureJwtBearer(JwtBearerOptions options)
        {
            options.Authority = oauth.Issuer;
            options.MetadataAddress = oauth.MetadataAddress ?? $"{oauth.Issuer}/.well-known/openid-configuration";
            options.RequireHttpsMetadata = !development;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = oauth.Issuer,
                ValidateAudience = true,
                ValidAudience = Security.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                ValidTypes = ["at+jwt"],
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        }

        void ConfigureAuthorization(AuthorizationOptions options)
        {
            foreach (var scope in new[] { Security.ManageBooks, Security.ManageAuthors })
            {
                options.AddPolicy(scope, policy => ConfigureManagementPolicy(policy, scope));
            }

            options.AddPolicy(Security.SearchBooks, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("client_id", oauth.SearchClientId)
                .RequireClaim("sub")
                .RequireAssertion(context => HasScope(context.User, Security.SearchBooks)));

            void ConfigureManagementPolicy(AuthorizationPolicyBuilder policy, string scope)
            {
                policy.RequireAuthenticatedUser()
                    .RequireClaim("client_id", oauth.ManagementClientId)
                    .RequireAssertion(context => ManagementRequirement(context, scope));
            }

            static bool ManagementRequirement(AuthorizationHandlerContext context, string scope)
            {
                return !context.User.HasClaim(claim => claim.Type == "sub")
                    && HasScope(context.User, scope);
            }
        }
    }

    private static bool HasScope(ClaimsPrincipal user, string scope)
    {
        foreach (var claim in user.FindAll("scope"))
        {
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (scopes.Contains(scope, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
