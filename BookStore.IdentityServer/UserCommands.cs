using System.Text;
using Microsoft.AspNetCore.Identity;

namespace BookStore.IdentityServer;

internal static class UserCommands
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length != 3 || args[0] is not ("create" or "reset-password") || args[1] != "--username"
            || string.IsNullOrWhiteSpace(args[2]))
        {
            Console.Error.WriteLine("Usage: users create|reset-password --username NAME (password is read securely from the terminal or one line of stdin)");
            return 1;
        }
        var password = await ReadPasswordAsync();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        IdentityResult result;
        if (args[0] == "create")
        {
            result = await users.CreateAsync(new IdentityUser(args[2]), password);
        }
        else
        {
            var user = await users.FindByNameAsync(args[2]);
            if (user is null)
            {
                Console.Error.WriteLine("User not found.");
                return 1;
            }
            var token = await users.GeneratePasswordResetTokenAsync(user);
            result = await users.ResetPasswordAsync(user, token, password);
            if (result.Succeeded)
            {
                await users.ResetAccessFailedCountAsync(user);
                await users.SetLockoutEndDateAsync(user, null);
            }
        }
        if (result.Succeeded)
        {
            Console.WriteLine(args[0] == "create" ? "User created." : "Password reset.");
            return 0;
        }
        foreach (var error in result.Errors)
        {
            Console.Error.WriteLine(error.Description);
        }
        return 1;
    }

    private static async Task<string> ReadPasswordAsync()
    {
        if (Console.IsInputRedirected)
        {
            return await Console.In.ReadLineAsync() ?? string.Empty;
        }
        Console.Write("Password: ");
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
        Console.WriteLine();
        return password.ToString();
    }
}
