using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IIndiaAssetService"/>.
/// Each India asset is stored as a row in <c>Assets</c> (AssetType = 'IndiaAsset')
/// joined with a row in <c>IndiaAssets</c>.
/// BankOrPlatform is stored in <c>Assets.Provider</c>.
/// </summary>
public sealed class SqliteIndiaAssetService(
    FamilyVaultDbContext dbContext,
    ILogger<SqliteIndiaAssetService> logger) : IIndiaAssetService
{
    private const string AssetType = "IndiaAsset";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IndiaAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<IndiaAssetRow>(
            """
            SELECT a.Id, a.Provider AS BankOrPlatform, ia.Category, ia.AccountType, ia.Repatriation, ia.Nominee
              FROM IndiaAssets ia
              JOIN Assets a ON ia.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<IndiaAssetResponse> AddAsync(
        string userId,
        IndiaAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Provider, Country, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Provider, 'India', 1, @CreatedAt)
            """,
            new { Id = id, UserId = userId, AssetType, Provider = request.BankOrPlatform, CreatedAt = DateTime.UtcNow.ToString("o") },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO IndiaAssets (Id, Category, AccountType, Repatriation, Nominee)
            VALUES (@Id, @Category, @AccountType, @Repatriation, @Nominee)
            """,
            new
            {
                Id = id,
                Category = request.Category.ToString(),
                AccountType = request.AccountType.ToString(),
                Repatriation = request.Repatriation.ToString(),
                request.Nominee
            },
            tx);

        tx.Commit();

        logger.LogInformation(
            "India asset {AssetId} added for user {UserId} (category={Category}, platform={Platform}, accountType={AccountType})",
            id, userId, request.Category, LogSanitizer.Sanitize(request.BankOrPlatform), request.AccountType);

        return BuildResponse(Guid.Parse(id), request.BankOrPlatform, request.Category,
            request.AccountType, request.Repatriation, request.Nominee);
    }

    /// <inheritdoc/>
    public async Task<IndiaAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        IndiaAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (exists == 0)
        {
            logger.LogWarning("Update requested for non-existent India asset {AssetId} for user {UserId}", id, userId);
            return null;
        }

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Provider = @Provider WHERE Id = @Id AND UserId = @UserId",
            new { Provider = request.BankOrPlatform, Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE IndiaAssets
               SET Category = @Category,
                   AccountType = @AccountType,
                   Repatriation = @Repatriation,
                   Nominee = @Nominee
             WHERE Id = @Id
            """,
            new
            {
                Category = request.Category.ToString(),
                AccountType = request.AccountType.ToString(),
                Repatriation = request.Repatriation.ToString(),
                request.Nominee,
                Id = id.ToString()
            },
            tx);

        tx.Commit();

        logger.LogInformation("India asset {AssetId} updated for user {UserId}", id, userId);

        return BuildResponse(id, request.BankOrPlatform, request.Category,
            request.AccountType, request.Repatriation, request.Nominee);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var affected = await conn.ExecuteAsync(
            "DELETE FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (affected > 0)
            logger.LogInformation("India asset {AssetId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent India asset {AssetId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(IndiaAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankOrPlatform))
            throw new IndiaAssetValidationException("Bank / platform is required.");
        if (request.BankOrPlatform.Length > 150)
            throw new IndiaAssetValidationException("Bank / platform must not exceed 150 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new IndiaAssetValidationException("Nominee name must not exceed 100 characters.");

        if (request.AccountType == NriAccountType.NRE &&
            request.Repatriation != RepatriationStatus.FullyRepatriable)
            throw new IndiaAssetValidationException("NRE accounts must be marked as Fully Repatriable.");

        if (request.AccountType == NriAccountType.NRO &&
            request.Repatriation != RepatriationStatus.Limited)
            throw new IndiaAssetValidationException("NRO accounts must be marked as Limited repatriation.");
    }

    private static IndiaAssetResponse MapToResponse(IndiaAssetRow row) =>
        BuildResponse(
            Guid.Parse(row.Id),
            row.BankOrPlatform,
            Enum.Parse<IndiaAssetCategory>(row.Category, ignoreCase: true),
            Enum.Parse<NriAccountType>(row.AccountType, ignoreCase: true),
            Enum.Parse<RepatriationStatus>(row.Repatriation, ignoreCase: true),
            row.Nominee);

    private static IndiaAssetResponse BuildResponse(
        Guid id,
        string bankOrPlatform,
        IndiaAssetCategory category,
        NriAccountType accountType,
        RepatriationStatus repatriation,
        string? nominee) =>
        new(
            Id: id,
            Category: category,
            BankOrPlatform: bankOrPlatform,
            AccountType: accountType,
            Repatriation: repatriation,
            Nominee: nominee,
            IsTaxableInIndia: accountType == NriAccountType.NRO,
            IsRepatriable: accountType == NriAccountType.NRE);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class IndiaAssetRow
    {
        public string Id { get; init; } = "";
        public string BankOrPlatform { get; init; } = "";
        public string Category { get; init; } = "";
        public string AccountType { get; init; } = "";
        public string Repatriation { get; init; } = "";
        public string? Nominee { get; init; }
    }
}
