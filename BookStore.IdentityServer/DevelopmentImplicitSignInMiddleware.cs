using BookStore.Models;
using Microsoft.AspNetCore.Identity;

namespace BookStore.IdentityServer;

public sealed class DevelopmentImplicitSignInMiddleware
{
    private readonly RequestDelegate next;
    private readonly string searchClientId;

    public DevelopmentImplicitSignInMiddleware(RequestDelegate next, string searchClientId)
    {
        this.next = next;
        this.searchClientId = searchClientId;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<IdentityUser> users,
        RoleManager<IdentityRole> roles,
        SignInManager<IdentityUser> signIn)
    {
        if (context.User.Identity?.IsAuthenticated != true && IsSearchAuthorizationRequest(context.Request, searchClientId))
        {
            var user = await FindOrCreateSearchUserAsync(users, roles, context.RequestAborted);
            await signIn.SignInAsync(user, isPersistent: false);
        }

        await next(context);
    }

    public static bool IsSearchAuthorizationRequest(HttpRequest request, string searchClientId)
    {
        if (!request.Path.Equals("/connect/authorize")
            || request.Query["client_id"] != searchClientId
            || request.Query["response_type"] != "token")
        {
            return false;
        }

        return request.Query["scope"]
            .SelectMany(value => (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(Security.SearchBooks, StringComparer.Ordinal);
    }

    private static async Task<IdentityUser> FindOrCreateSearchUserAsync(
        UserManager<IdentityUser> users,
        RoleManager<IdentityRole> roles,
        CancellationToken cancellationToken)
    {
        var role = await roles.FindByNameAsync(Security.SearchRole);
        if (role is null)
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(Security.SearchRole));
            EnsureSucceeded(roleResult, "create the Development search role");
        }

        var user = await users.FindByNameAsync(Security.DevelopmentSearchUsername);
        if (user is null)
        {
            user = new IdentityUser(Security.DevelopmentSearchUsername)
            {
                EmailConfirmed = true
            };
            var userResult = await users.CreateAsync(user);
            EnsureSucceeded(userResult, "create the Development search user");
        }

        if (!await users.IsInRoleAsync(user, Security.SearchRole))
        {
            var assignmentResult = await users.AddToRoleAsync(user, Security.SearchRole);
            EnsureSucceeded(assignmentResult, "assign the Development search role");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not {operation}: {string.Join(" ", result.Errors.Select(error => error.Description))}");
        }
    }
}
