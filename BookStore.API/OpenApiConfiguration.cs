using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BookStore.API;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddBookStoreOpenApi(this IServiceCollection services, OAuthSettings oauth)
    {
        services.AddOpenApi(ConfigureOpenApi);
        return services;

        void ConfigureOpenApi(OpenApiOptions options)
        {
            options.AddDocumentTransformer(ConfigureDocument);
            options.AddOperationTransformer(ConfigureOperation);
        }

        Task ConfigureDocument(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo { Title = "Bookstore API", Version = "v1" };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["management"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = $"Client ID: {oauth.ManagementClientId}. Enter the configured management secret.",
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri($"{oauth.Issuer}/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                [Security.ManageBooks] = "Manage books",
                                [Security.ManageAuthors] = "Manage authors"
                            }
                        }
                    }
                },
                ["search"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = $"Client ID: {oauth.SearchClientId}. Sign in through IdentityServer to search books.",
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{oauth.Issuer}/connect/authorize"),
                            Scopes = new Dictionary<string, string>
                            {
                                [Security.SearchBooks] = "Search books"
                            }
                        }
                    }
                }
            };
            return Task.CompletedTask;
        }

        Task ConfigureOperation(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var scope = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Select(attribute => attribute.Policy)
                .FirstOrDefault(policy => policy is Security.ManageBooks or Security.ManageAuthors or Security.SearchBooks);
            if (scope is null)
            {
                return Task.CompletedTask;
            }

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [
                    new OpenApiSecuritySchemeReference(
                        scope == Security.SearchBooks ? "search" : "management",
                        context.Document)
                    ] = [scope]
                }
            ];
            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Invalid request" });
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "A valid access token is required" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "The client or scope is not permitted" });
            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Resource not found" });
            if (scope == Security.ManageAuthors)
            {
                operation.Responses.TryAdd("409", new OpenApiResponse { Description = "The author still has books" });
            }

            return Task.CompletedTask;
        }
    }
}
