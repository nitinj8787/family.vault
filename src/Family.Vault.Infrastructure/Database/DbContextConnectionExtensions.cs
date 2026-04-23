using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Family.Vault.Infrastructure.Database;

internal static class DbContextConnectionExtensions
{
    internal static async Task<DbConnection> OpenConnectionAsync(
        this FamilyVaultDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        return conn;
    }
}
