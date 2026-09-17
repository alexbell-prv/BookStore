using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Infrastructure;

public sealed class RequestCancellationContext
{
    private CancellationToken cancellationToken;

    public CancellationToken CancellationToken
    {
        get
        {
            return cancellationToken;
        }
    }

    internal void Set(CancellationToken token)
    {
        cancellationToken = token;
    }

    internal void Reset()
    {
        cancellationToken = CancellationToken.None;
    }
}

public sealed class RequestCancellationMiddleware
{
    private const int ClientClosedRequestStatusCode = 499;
    private readonly RequestDelegate next;

    public RequestCancellationMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestCancellationContext cancellationContext)
    {
        var requestCancellationToken = httpContext.RequestAborted;
        cancellationContext.Set(requestCancellationToken);

        try
        {
            await next(httpContext);
        }
        catch (OperationCanceledException) when (requestCancellationToken.IsCancellationRequested)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = ClientClosedRequestStatusCode;
            }
        }
        finally
        {
            cancellationContext.Reset();
        }
    }
}

public static class RequestCancellationExtensions
{
    public static IServiceCollection AddRequestCancellation(this IServiceCollection services)
    {
        services.AddScoped<RequestCancellationContext>();
        return services;
    }

    public static IApplicationBuilder UseRequestCancellation(this IApplicationBuilder application)
    {
        return application.UseMiddleware<RequestCancellationMiddleware>();
    }
}
