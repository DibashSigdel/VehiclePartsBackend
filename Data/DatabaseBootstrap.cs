using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace VehiclePartsBackend.Data;

public static class DatabaseBootstrap
{
    public static async Task EnsurePostgreSqlReadyAsync(string connectionString, ILogger logger)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database
            ?? throw new InvalidOperationException("Connection string must include Database=.");

        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        checkCmd.Parameters.AddWithValue("name", databaseName);
        var exists = await checkCmd.ExecuteScalarAsync() is not null;

        if (!exists)
        {
            logger.LogWarning("Database '{Database}' not found. Creating it now...", databaseName);
            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{Database}' created.", databaseName);
        }
    }

    public static async Task InitializeAsync(AppDbContext dbContext, ILogger logger, bool seedDevelopmentData)
    {
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        await EnsurePostgreSqlReadyAsync(connectionString, logger);

        logger.LogInformation("Applying PostgreSQL EF Core migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("PostgreSQL database is up to date.");

        if (seedDevelopmentData)
        {
            await DevDataSeeder.SeedAsync(dbContext, logger);
        }
    }
}
