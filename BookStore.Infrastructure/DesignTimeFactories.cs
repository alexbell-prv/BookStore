using BookStore.Infrastructure.Identity;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookStore.Infrastructure;

// Model generation does not require a running server. Database updates use the supplied environment connection.
internal static class DesignTimeOptions
{
    public static DbContextOptions<T> Create<T>(string connectionName, string database, string history) where T : DbContext
    {
        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{connectionName}")
            ?? $"Server=localhost,1433;Database={database};Encrypt=True;TrustServerCertificate=True";
        var optionsBuilder = new DbContextOptionsBuilder<T>();
        optionsBuilder.UseSqlServer(connectionString, ConfigureSql);
        return optionsBuilder.Options;

        void ConfigureSql(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sql)
        {
            DatabaseRegistration.ConfigureSql(sql, history);
        }
    }
}

public sealed class BookStoreDesignTimeFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var options = DesignTimeOptions.Create<BookStoreDbContext>("BookStore", "BookStore", "__BookStoreMigrations");
        return new BookStoreDbContext(options);
    }
}

public sealed class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationIdentityDbContext>
{
    public ApplicationIdentityDbContext CreateDbContext(string[] args)
    {
        var options = DesignTimeOptions.Create<ApplicationIdentityDbContext>("Identity", "BookStore.Identity", "__IdentityMigrations");
        return new ApplicationIdentityDbContext(options);
    }
}

public sealed class ConfigurationDesignTimeFactory : IDesignTimeDbContextFactory<IdentityConfigurationDbContext>
{
    public IdentityConfigurationDbContext CreateDbContext(string[] args)
    {
        var options = DesignTimeOptions.Create<IdentityConfigurationDbContext>(
            "Identity",
            "BookStore.Identity",
            "__ConfigurationMigrations");
        return new IdentityConfigurationDbContext(options)
        {
            StoreOptions = new Duende.IdentityServer.EntityFramework.Options.ConfigurationStoreOptions()
        };
    }
}

public sealed class OperationalDesignTimeFactory : IDesignTimeDbContextFactory<IdentityOperationalDbContext>
{
    public IdentityOperationalDbContext CreateDbContext(string[] args)
    {
        var options = DesignTimeOptions.Create<IdentityOperationalDbContext>(
            "Identity",
            "BookStore.Identity",
            "__OperationalMigrations");
        return new IdentityOperationalDbContext(options)
        {
            StoreOptions = new Duende.IdentityServer.EntityFramework.Options.OperationalStoreOptions()
        };
    }
}
