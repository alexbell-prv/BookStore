using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStore.IdentityServer.Pages.Account;

public sealed class ErrorModel : PageModel
{
    private readonly IIdentityServerInteractionService interaction;

    public ErrorModel(IIdentityServerInteractionService interaction)
    {
        this.interaction = interaction;
    }

    public string Message { get; private set; } = "Please return to the application and try again.";

    public async Task OnGetAsync(string? errorId)
    {
        if (errorId is not null)
        {
            var error = await interaction.GetErrorContextAsync(errorId, HttpContext.RequestAborted);
            if (error is not null)
            {
                Message = error.ErrorDescription ?? error.Error;
            }
        }
    }
}
