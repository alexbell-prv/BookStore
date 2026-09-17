using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BookStore.Infrastructure;

public sealed class DatabaseHealthCheck<TContext> : IHealthCheck where TContext : DbContext
{
    private readonly IServiceScopeFactory scopeFactory;

    public DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<TContext>().Database;
        try
        {
            var canConnect = await database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return HealthCheckResult.Healthy();
            }

            return HealthCheckResult.Unhealthy("Database unavailable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database unavailable.", exception);
        }
    }
}
