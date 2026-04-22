using System.Data;
using Microsoft.Data.Sqlite;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// Creates open-able <see cref="IDbConnection"/> instances backed by a SQLite file.
/// Inject this as a singleton and call <see cref="CreateConnection"/> per unit of work.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <summary>Returns a new, unopened <see cref="SqliteConnection"/>.</summary>
    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
