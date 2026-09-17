namespace BookStore.IdentityServer;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;
    private readonly string swaggerOrigin;

    public SecurityHeadersMiddleware(RequestDelegate next, string swaggerRedirectUri)
    {
        this.next = next;
        swaggerOrigin = new Uri(swaggerRedirectUri).GetLeftPart(UriPartial.Authority);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (context.Request.Path.StartsWithSegments("/Account"))
        {
            context.Response.Headers.CacheControl = "no-cache, no-store";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers["Content-Security-Policy"] =
                $"default-src 'self'; frame-ancestors 'none'; form-action 'self' {swaggerOrigin}; base-uri 'self'";
        }

        await next(context);
    }
}
