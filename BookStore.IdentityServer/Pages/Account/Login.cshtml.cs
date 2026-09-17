using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStore.IdentityServer.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> signIn;
    private readonly IIdentityServerInteractionService interaction;

    public LoginModel(SignInManager<IdentityUser> signIn, IIdentityServerInteractionService interaction)
    {
        this.signIn = signIn;
        this.interaction = interaction;
    }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    [BindProperty]
    public Credentials Input { get; set; } = new();

    public IActionResult OnGet()
    {
        return ValidReturnUrl() ? Page() : BadRequest("Invalid return URL.");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ValidReturnUrl())
        {
            return BadRequest("Invalid return URL.");
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var result = await signIn.PasswordSignInAsync(Input.Username, Input.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return Redirect(ReturnUrl);
        }
        ModelState.AddModelError(string.Empty, "Unable to sign in. Check your credentials or try again later.");
        return Page();
    }

    private bool ValidReturnUrl()
    {
        return Url.IsLocalUrl(ReturnUrl) || interaction.IsValidReturnUrl(ReturnUrl);
    }

    public sealed class Credentials
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
