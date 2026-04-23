using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IBankAccountService"/>.
/// Each bank account is stored as a row in <c>Assets</c> (AssetType = 'Bank')
/// joined with a row in <c>BankAccounts</c>.
/// </summary>
public sealed class SqliteBankAccountService(
    FamilyVaultDbContext dbContext,
    ILogger<SqliteBankAccountService> logger) : IBankAccountService
{
    private const string AssetType = "Bank";

    // -----------------------------------------------------------------
    // Query
    // -----------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BankAccountResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<BankAccountRow>(
            """
            SELECT a.Id, a.Name AS BankName, ba.AccountNumber, ba.AccountType, ba.Nominee
              FROM BankAccounts ba
              JOIN Assets a ON ba.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    // -----------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<BankAccountResponse> AddAsync(
        string userId,
        BankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureNoDuplicateAsync(userId, excludeId: null, request, cancellationToken);

        var id = Guid.NewGuid().ToString();

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
                using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Name, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Name, 1, @CreatedAt)
            """,
            new { Id = id, UserId = userId, AssetType, Name = request.BankName, CreatedAt = DateTime.UtcNow.ToString("o") },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO BankAccounts (Id, AccountNumber, AccountType, Nominee)
            VALUES (@Id, @AccountNumber, @AccountType, @Nominee)
            """,
            new { Id = id, AccountNumber = request.AccountNumber, AccountType = request.AccountType.ToString(), request.Nominee },
            tx);

        tx.Commit();

        logger.LogInformation(
            "Bank account {AccountId} added for user {UserId} (bankName={BankName}, accountType={AccountType})",
            id, userId, LogSanitizer.Sanitize(request.BankName), request.AccountType);

        return new BankAccountResponse(
            Id: Guid.Parse(id),
            BankName: request.BankName,
            AccountType: request.AccountType,
            MaskedAccountNumber: MaskAccountNumber(request.AccountNumber),
            Nominee: request.Nominee);
    }

    /// <inheritdoc/>
    public async Task<BankAccountResponse?> UpdateAsync(
        string userId,
        Guid id,
        BankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (exists == 0)
        {
            logger.LogWarning("Update requested for non-existent bank account {AccountId} for user {UserId}", id, userId);
            return null;
        }

        await EnsureNoDuplicateAsync(userId, excludeId: id, request, cancellationToken);

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Name = @Name WHERE Id = @Id AND UserId = @UserId",
            new { Name = request.BankName, Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE BankAccounts
               SET AccountNumber = @AccountNumber,
                   AccountType = @AccountType,
                   Nominee = @Nominee
             WHERE Id = @Id
            """,
            new { AccountNumber = request.AccountNumber, AccountType = request.AccountType.ToString(), request.Nominee, Id = id.ToString() },
            tx);

        tx.Commit();

        logger.LogInformation("Bank account {AccountId} updated for user {UserId}", id, userId);

        return new BankAccountResponse(
            Id: id,
            BankName: request.BankName,
            AccountType: request.AccountType,
            MaskedAccountNumber: MaskAccountNumber(request.AccountNumber),
            Nominee: request.Nominee);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        // Deleting from Assets cascades to BankAccounts.
        var affected = await conn.ExecuteAsync(
            "DELETE FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (affected > 0)
            logger.LogInformation("Bank account {AccountId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent bank account {AccountId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(BankAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankName))
            throw new BankAccountValidationException("Bank name is required.");
        if (request.BankName.Length > 150)
            throw new BankAccountValidationException("Bank name must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new BankAccountValidationException("Account number is required.");
        if (request.AccountNumber.Length > 50)
            throw new BankAccountValidationException("Account number must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new BankAccountValidationException("Nominee name must not exceed 100 characters.");
    }

    private async Task EnsureNoDuplicateAsync(
        string userId,
        Guid? excludeId,
        BankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var sql = excludeId is null
            ? """
              SELECT COUNT(1) FROM BankAccounts ba
              JOIN Assets a ON ba.Id = a.Id
              WHERE a.UserId = @UserId
                AND LOWER(a.Name) = LOWER(@BankName)
                AND LOWER(ba.AccountNumber) = LOWER(@AccountNumber)
              """
            : """
              SELECT COUNT(1) FROM BankAccounts ba
              JOIN Assets a ON ba.Id = a.Id
              WHERE a.UserId = @UserId
                AND a.Id != @ExcludeId
                AND LOWER(a.Name) = LOWER(@BankName)
                AND LOWER(ba.AccountNumber) = LOWER(@AccountNumber)
              """;

        var count = await conn.ExecuteScalarAsync<int>(sql,
            new { UserId = userId, BankName = request.BankName, AccountNumber = request.AccountNumber, ExcludeId = excludeId?.ToString() });

        if (count > 0)
            throw new BankAccountValidationException(
                "An account with the same bank name and account number already exists.");
    }

    private static BankAccountResponse MapToResponse(BankAccountRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            BankName: row.BankName,
            AccountType: Enum.Parse<BankAccountType>(row.AccountType, ignoreCase: true),
            MaskedAccountNumber: MaskAccountNumber(row.AccountNumber),
            Nominee: row.Nominee);

    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return new string('•', accountNumber.Length);

        return new string('•', accountNumber.Length - 4) + accountNumber[^4..];
    }

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class BankAccountRow
    {
        public string Id { get; init; } = "";
        public string BankName { get; init; } = "";
        public string AccountNumber { get; init; } = "";
        public string AccountType { get; init; } = "";
        public string? Nominee { get; init; }
    }
}
