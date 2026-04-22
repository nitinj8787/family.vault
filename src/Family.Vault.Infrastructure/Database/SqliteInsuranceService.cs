using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IInsuranceService"/>.
/// Each insurance policy is stored as a row in <c>Assets</c> (AssetType = 'Insurance')
/// joined with a row in <c>InsurancePolicies</c>.
/// Provider is stored in <c>Assets.Provider</c>.
/// </summary>
public sealed class SqliteInsuranceService(
    SqliteConnectionFactory connectionFactory,
    ILogger<SqliteInsuranceService> logger) : IInsuranceService
{
    private const string AssetType = "Insurance";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InsuranceResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<InsurancePolicyRow>(
            """
            SELECT a.Id, a.Provider, ip.PolicyNumber, ip.PolicyType, ip.Coverage, ip.ClaimContact, ip.Nominee
              FROM InsurancePolicies ip
              JOIN Assets a ON ip.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<InsuranceResponse> AddAsync(
        string userId,
        InsuranceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Provider, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Provider, 1, @CreatedAt)
            """,
            new { Id = id, UserId = userId, AssetType, Provider = request.Provider, CreatedAt = DateTime.UtcNow.ToString("o") },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO InsurancePolicies (Id, PolicyNumber, PolicyType, Coverage, ClaimContact, Nominee)
            VALUES (@Id, @PolicyNumber, @PolicyType, @Coverage, @ClaimContact, @Nominee)
            """,
            new
            {
                Id = id,
                PolicyNumber = request.PolicyNumber,
                request.PolicyType,
                request.Coverage,
                request.ClaimContact,
                request.Nominee
            },
            tx);

        tx.Commit();

        logger.LogInformation(
            "Insurance policy {PolicyId} added for user {UserId} (provider={Provider}, policyType={PolicyType})",
            id, userId, LogSanitizer.Sanitize(request.Provider), LogSanitizer.Sanitize(request.PolicyType));

        return new InsuranceResponse(
            Id: Guid.Parse(id),
            Provider: request.Provider,
            PolicyType: request.PolicyType,
            PolicyNumber: request.PolicyNumber,
            Coverage: request.Coverage,
            Nominee: request.Nominee,
            ClaimContact: request.ClaimContact);
    }

    /// <inheritdoc/>
    public async Task<InsuranceResponse?> UpdateAsync(
        string userId,
        Guid id,
        InsuranceRequest request,
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
            logger.LogWarning("Update requested for non-existent insurance policy {PolicyId} for user {UserId}", id, userId);
            return null;
        }

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Provider = @Provider WHERE Id = @Id AND UserId = @UserId",
            new { Provider = request.Provider, Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE InsurancePolicies
               SET PolicyNumber = @PolicyNumber,
                   PolicyType = @PolicyType,
                   Coverage = @Coverage,
                   ClaimContact = @ClaimContact,
                   Nominee = @Nominee
             WHERE Id = @Id
            """,
            new
            {
                PolicyNumber = request.PolicyNumber,
                request.PolicyType,
                request.Coverage,
                request.ClaimContact,
                request.Nominee,
                Id = id.ToString()
            },
            tx);

        tx.Commit();

        logger.LogInformation("Insurance policy {PolicyId} updated for user {UserId}", id, userId);

        return new InsuranceResponse(
            Id: id,
            Provider: request.Provider,
            PolicyType: request.PolicyType,
            PolicyNumber: request.PolicyNumber,
            Coverage: request.Coverage,
            Nominee: request.Nominee,
            ClaimContact: request.ClaimContact);
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
            logger.LogInformation("Insurance policy {PolicyId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent insurance policy {PolicyId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(InsuranceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new InsuranceValidationException("Provider is required.");
        if (request.Provider.Length > 150)
            throw new InsuranceValidationException("Provider must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.PolicyType))
            throw new InsuranceValidationException("Policy type is required.");
        if (request.PolicyType.Length > 100)
            throw new InsuranceValidationException("Policy type must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.PolicyNumber))
            throw new InsuranceValidationException("Policy number is required.");
        if (request.PolicyNumber.Length > 60)
            throw new InsuranceValidationException("Policy number must not exceed 60 characters.");

        if (string.IsNullOrWhiteSpace(request.Coverage))
            throw new InsuranceValidationException("Coverage is required.");
        if (request.Coverage.Length > 120)
            throw new InsuranceValidationException("Coverage must not exceed 120 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new InsuranceValidationException("Nominee name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.ClaimContact))
            throw new InsuranceValidationException("Claim contact is required.");
        if (request.ClaimContact.Length > 120)
            throw new InsuranceValidationException("Claim contact must not exceed 120 characters.");
    }

    private static InsuranceResponse MapToResponse(InsurancePolicyRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            Provider: row.Provider,
            PolicyType: row.PolicyType,
            PolicyNumber: row.PolicyNumber,
            Coverage: row.Coverage,
            Nominee: row.Nominee,
            ClaimContact: row.ClaimContact);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class InsurancePolicyRow
    {
        public string Id { get; init; } = "";
        public string Provider { get; init; } = "";
        public string PolicyNumber { get; init; } = "";
        public string PolicyType { get; init; } = "";
        public string Coverage { get; init; } = "";
        public string ClaimContact { get; init; } = "";
        public string? Nominee { get; init; }
    }
}
