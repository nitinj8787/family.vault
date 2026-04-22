using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IUkAssetService"/>.
/// Each UK asset is stored as a row in <c>Assets</c> (AssetType = 'UkAsset')
/// joined with a row in <c>UkAssets</c>.
/// </summary>
public sealed class SqliteUkAssetService(
    SqliteConnectionFactory connectionFactory,
    ILogger<SqliteUkAssetService> logger) : IUkAssetService
{
    private const string AssetType = "UkAsset";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UkAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<UkAssetRow>(
            """
            SELECT a.Id, a.Provider, ua.Category, ua.AccountNumber, ua.Nominee, ua.TaxNotes
              FROM UkAssets ua
              JOIN Assets a ON ua.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<UkAssetResponse> AddAsync(
        string userId,
        UkAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Provider, Country, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Provider, 'UK', 1, @CreatedAt)
            """,
            new { Id = id, UserId = userId, AssetType, Provider = request.Provider, CreatedAt = DateTime.UtcNow.ToString("o") },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO UkAssets (Id, Category, AccountNumber, Nominee, TaxNotes)
            VALUES (@Id, @Category, @AccountNumber, @Nominee, @TaxNotes)
            """,
            new
            {
                Id = id,
                Category = request.Category.ToString(),
                request.AccountNumber,
                request.Nominee,
                request.TaxNotes
            },
            tx);

        tx.Commit();

        logger.LogInformation(
            "UK asset {AssetId} added for user {UserId} (category={Category}, provider={Provider})",
            id, userId, request.Category, LogSanitizer.Sanitize(request.Provider));

        return new UkAssetResponse(
            Id: Guid.Parse(id),
            Category: request.Category,
            Provider: request.Provider,
            MaskedAccountNumber: MaskAccountNumber(request.AccountNumber),
            Nominee: request.Nominee,
            TaxNotes: request.TaxNotes);
    }

    /// <inheritdoc/>
    public async Task<UkAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        UkAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        using var conn = connectionFactory.CreateConnection();
        conn.Open();

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (exists == 0)
        {
            logger.LogWarning("Update requested for non-existent UK asset {AssetId} for user {UserId}", id, userId);
            return null;
        }

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Provider = @Provider WHERE Id = @Id AND UserId = @UserId",
            new { Provider = request.Provider, Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE UkAssets
               SET Category = @Category,
                   AccountNumber = @AccountNumber,
                   Nominee = @Nominee,
                   TaxNotes = @TaxNotes
             WHERE Id = @Id
            """,
            new
            {
                Category = request.Category.ToString(),
                request.AccountNumber,
                request.Nominee,
                request.TaxNotes,
                Id = id.ToString()
            },
            tx);

        tx.Commit();

        logger.LogInformation("UK asset {AssetId} updated for user {UserId}", id, userId);

        return new UkAssetResponse(
            Id: id,
            Category: request.Category,
            Provider: request.Provider,
            MaskedAccountNumber: MaskAccountNumber(request.AccountNumber),
            Nominee: request.Nominee,
            TaxNotes: request.TaxNotes);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (affected > 0)
            logger.LogInformation("UK asset {AssetId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent UK asset {AssetId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(UkAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new UkAssetValidationException("Provider is required.");
        if (request.Provider.Length > 150)
            throw new UkAssetValidationException("Provider must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new UkAssetValidationException("Account number is required.");
        if (request.AccountNumber.Length > 50)
            throw new UkAssetValidationException("Account number must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new UkAssetValidationException("Nominee name must not exceed 100 characters.");

        if (request.TaxNotes is { Length: > 500 })
            throw new UkAssetValidationException("Tax notes must not exceed 500 characters.");
    }

    private static UkAssetResponse MapToResponse(UkAssetRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            Category: Enum.Parse<UkAssetCategory>(row.Category, ignoreCase: true),
            Provider: row.Provider,
            MaskedAccountNumber: MaskAccountNumber(row.AccountNumber),
            Nominee: row.Nominee,
            TaxNotes: row.TaxNotes);

    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return new string('•', accountNumber.Length);

        return new string('•', accountNumber.Length - 4) + accountNumber[^4..];
    }

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class UkAssetRow
    {
        public string Id { get; init; } = "";
        public string Provider { get; init; } = "";
        public string Category { get; init; } = "";
        public string AccountNumber { get; init; } = "";
        public string? Nominee { get; init; }
        public string? TaxNotes { get; init; }
    }
}
