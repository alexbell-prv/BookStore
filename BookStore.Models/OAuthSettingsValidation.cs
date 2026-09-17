namespace BookStore.Models;

public static class OAuthSettingsValidation
{
    public static void Validate(this OAuthSettings settings, bool development)
    {
        settings.Issuer = settings.Issuer.TrimEnd('/');
        ValidateUrl(settings.Issuer, "OAuth:Issuer", development);
        ValidateUrl(settings.SwaggerRedirectUri, "OAuth:SwaggerRedirectUri", development);
        if (settings.MetadataAddress is not null)
        {
            ValidateUrl(settings.MetadataAddress, "OAuth:MetadataAddress", development);
        }
        if (string.IsNullOrWhiteSpace(settings.ManagementClientId) || string.IsNullOrWhiteSpace(settings.SearchClientId)
            || settings.ManagementClientId == settings.SearchClientId)
        {
            throw new InvalidOperationException("OAuth client IDs must be nonempty and distinct.");
        }
    }

    private static void ValidateUrl(string value, string name, bool development)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || (!development && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException($"{name} must be an absolute HTTP(S) URL; HTTPS is required outside Development.");
        }
    }
}
