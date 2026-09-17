using Microsoft.AspNetCore.Identity;

namespace BookStore.Infrastructure.Identity;

public static class IdentityOptionsConfiguration
{
    public static void Configure(IdentityOptions options)
    {
        options.Password.RequiredLength = 6;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.SignIn.RequireConfirmedAccount = false;
    }
}
