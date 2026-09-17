using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStore.IdentityServer.Pages.Account;

public sealed class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser> signIn;
    private readonly IIdentityServerInteractionService interaction;

    public LogoutModel(SignInManager<IdentityUser> signIn, IIdentityServerInteractionService interaction)
    {
        this.signIn = signIn;
        this.interaction = interaction;
    }

    [BindProperty(SupportsGet = true)]
    public string? LogoutId { get; set; }

    public bool SignedOut { get; private set; }
    public string? PostLogoutRedirectUri { get; private set; }
    public string? SignOutIframeUrl { get; private set; }

    public async Task OnPostAsync()
    {
        var context = await interaction.GetLogoutContextAsync(LogoutId, HttpContext.RequestAborted);
        await signIn.SignOutAsync();
        SignedOut = true;
        PostLogoutRedirectUri = context?.PostLogoutRedirectUri;
        SignOutIframeUrl = context?.SignOutIFrameUrl;
    }
}
