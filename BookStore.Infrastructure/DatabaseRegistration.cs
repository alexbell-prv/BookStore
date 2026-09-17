using BookStore.Infrastructure.Identity;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Infrastructure;

public static class DatabaseRegistration
{
    public static void ConfigureSql(SqlServerDbContextOptionsBuilder sql, string historyTable)
    {
        sql.MigrationsAssembly(typeof(BookStoreDbContext).Assembly.FullName)
            .MigrationsHistoryTable(historyTable)
            .EnableRetryOnFailure();
    }

    public static IServiceCollection AddBookStoreDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BookStoreDbContext>(ConfigureDatabase);
        services.AddScoped<IEfQueryExecutor, EfQueryExecutor>();
        return services;

        void ConfigureDatabase(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(connectionString, ConfigureBookStoreSql);
        }
    }

    public static IServiceCollection AddIdentityDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationIdentityDbContext>(ConfigureDatabase);
        return services;

        void ConfigureDatabase(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(connectionString, ConfigureIdentitySql);
        }
    }

    public static async Task MigrateIdentityAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await services.GetRequiredService<ApplicationIdentityDbContext>().Database.MigrateAsync(cancellationToken);
        await services.GetRequiredService<IdentityConfigurationDbContext>().Database.MigrateAsync(cancellationToken);
        await services.GetRequiredService<IdentityOperationalDbContext>().Database.MigrateAsync(cancellationToken);
    }

    private static void ConfigureBookStoreSql(SqlServerDbContextOptionsBuilder sql)
    {
        ConfigureSql(sql, "__BookStoreMigrations");
    }

    private static void ConfigureIdentitySql(SqlServerDbContextOptionsBuilder sql)
    {
        ConfigureSql(sql, "__IdentityMigrations");
    }
}
