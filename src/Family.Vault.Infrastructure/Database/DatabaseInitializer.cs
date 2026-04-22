using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// Initializes the SQLite database by executing the embedded <c>schema.sql</c> script.
/// Safe to call on every application start — all DDL statements use <c>IF NOT EXISTS</c>.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Opens (or creates) the SQLite database at the path encoded in
    /// <paramref name="connectionString"/> and applies the schema.
    /// </summary>
    public static async Task InitializeAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var sql = LoadSchema();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Enable WAL mode for better concurrent read/write performance.
        await connection.ExecuteAsync("PRAGMA journal_mode = WAL;");
        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");

        // Execute the full schema script.  Each statement is separated by the
        // semicolon that ends it; Dapper handles multi-statement execution.
        await connection.ExecuteAsync(sql);

        logger.LogInformation("SQLite database schema initialized successfully.");
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static string LoadSchema()
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        const string resourceName = "Family.Vault.Infrastructure.Database.schema.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'. " +
                "Ensure the file is included as an EmbeddedResource in the project file.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
