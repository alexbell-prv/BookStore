using Microsoft.Data.SqlClient;

namespace BookStore.Infrastructure;

public static class DatabaseConnection
{
    public static string Create(string? configuredConnectionString, string? password, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException($"Set ConnectionStrings:{connectionName}.");
        }

        var builder = new SqlConnectionStringBuilder(configuredConnectionString);
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
        }

        if (string.IsNullOrWhiteSpace(builder.Password))
        {
            throw new InvalidOperationException(
                $"Set MSSQL_SA_PASSWORD or provide a password in ConnectionStrings:{connectionName}.");
        }

        return builder.ConnectionString;
    }
}
