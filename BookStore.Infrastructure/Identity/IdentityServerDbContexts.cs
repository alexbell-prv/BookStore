using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Identity;

public sealed class IdentityConfigurationDbContext : ConfigurationDbContext<IdentityConfigurationDbContext>
{
    public IdentityConfigurationDbContext(DbContextOptions<IdentityConfigurationDbContext> options)
        : base(options)
    {
    }
}

public sealed class IdentityOperationalDbContext : PersistedGrantDbContext<IdentityOperationalDbContext>
{
    public IdentityOperationalDbContext(DbContextOptions<IdentityOperationalDbContext> options)
        : base(options)
    {
    }
}
