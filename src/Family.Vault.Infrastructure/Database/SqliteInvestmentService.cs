using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IInvestmentService"/>.
/// Each investment is stored as a row in <c>Assets</c> (AssetType = 'Investment')
/// joined with a row in <c>Investments</c>.
/// </summary>
public sealed class SqliteInvestmentService(
    FamilyVaultDbContext dbContext,
    ILogger<SqliteInvestmentService> logger) : IInvestmentService
{
    private const string AssetType = "Investment";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InvestmentResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<InvestmentRow>(
            """
            SELECT a.Id, i.Platform, i.InvestmentType, i.AccountId, i.Nominee
              FROM Investments i
              JOIN Assets a ON i.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<InvestmentResponse> AddAsync(
        string userId,
        InvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Provider, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Provider, 1, @CreatedAt)
            """,
            new { Id = id, UserId = userId, AssetType, Provider = request.Platform, CreatedAt = DateTime.UtcNow.ToString("o") },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO Investments (Id, Platform, InvestmentType, AccountId, Nominee)
            VALUES (@Id, @Platform, @InvestmentType, @AccountId, @Nominee)
            """,
            new
            {
                Id = id,
                request.Platform,
                InvestmentType = request.Type.ToString(),
                AccountId = request.AccountId,
                request.Nominee
            },
            tx);

        tx.Commit();

        logger.LogInformation(
            "Investment {InvestmentId} added for user {UserId} (platform={Platform}, type={Type})",
            id, userId, LogSanitizer.Sanitize(request.Platform), request.Type);

        return new InvestmentResponse(
            Id: Guid.Parse(id),
            Platform: request.Platform,
            Type: request.Type,
            MaskedAccountId: MaskAccountId(request.AccountId),
            Nominee: request.Nominee);
    }

    /// <inheritdoc/>
    public async Task<InvestmentResponse?> UpdateAsync(
        string userId,
        Guid id,
        InvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (exists == 0)
        {
            logger.LogWarning("Update requested for non-existent investment {InvestmentId} for user {UserId}", id, userId);
            return null;
        }

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Provider = @Provider WHERE Id = @Id AND UserId = @UserId",
            new { Provider = request.Platform, Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE Investments
               SET Platform = @Platform,
                   InvestmentType = @InvestmentType,
                   AccountId = @AccountId,
                   Nominee = @Nominee
             WHERE Id = @Id
            """,
            new
            {
                request.Platform,
                InvestmentType = request.Type.ToString(),
                AccountId = request.AccountId,
                request.Nominee,
                Id = id.ToString()
            },
            tx);

        tx.Commit();

        logger.LogInformation("Investment {InvestmentId} updated for user {UserId}", id, userId);

        return new InvestmentResponse(
            Id: id,
            Platform: request.Platform,
            Type: request.Type,
            MaskedAccountId: MaskAccountId(request.AccountId),
            Nominee: request.Nominee);
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
            logger.LogInformation("Investment {InvestmentId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent investment {InvestmentId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(InvestmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform))
            throw new InvestmentValidationException("Platform is required.");
        if (request.Platform.Length > 150)
            throw new InvestmentValidationException("Platform must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountId))
            throw new InvestmentValidationException("Account ID is required.");
        if (request.AccountId.Length > 50)
            throw new InvestmentValidationException("Account ID must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new InvestmentValidationException("Nominee name must not exceed 100 characters.");
    }

    private static InvestmentResponse MapToResponse(InvestmentRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            Platform: row.Platform,
            Type: Enum.Parse<InvestmentType>(row.InvestmentType, ignoreCase: true),
            MaskedAccountId: MaskAccountId(row.AccountId),
            Nominee: row.Nominee);

    private static string MaskAccountId(string accountId)
    {
        if (accountId.Length <= 4)
            return new string('•', accountId.Length);

        return new string('•', accountId.Length - 4) + accountId[^4..];
    }

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class InvestmentRow
    {
        public string Id { get; init; } = "";
        public string Platform { get; init; } = "";
        public string InvestmentType { get; init; } = "";
        public string AccountId { get; init; } = "";
        public string? Nominee { get; init; }
    }
}
