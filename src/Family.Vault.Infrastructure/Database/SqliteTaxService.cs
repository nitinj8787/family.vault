using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="ITaxService"/>.
/// </summary>
public sealed class SqliteTaxService(
    SqliteConnectionFactory connectionFactory,
    ILogger<SqliteTaxService> logger) : ITaxService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaxEntryResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<TaxEntryRow>(
            "SELECT Id, Country, IncomeType, TaxPaid, DeclaredInUK FROM TaxEntries WHERE UserId = @UserId",
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<TaxEntryResponse> AddAsync(
        string userId,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO TaxEntries (Id, UserId, Country, IncomeType, TaxPaid, DeclaredInUK)
            VALUES (@Id, @UserId, @Country, @IncomeType, @TaxPaid, @DeclaredInUK)
            """,
            new
            {
                Id = id,
                UserId = userId,
                request.Country,
                IncomeType = request.IncomeType.ToString(),
                TaxPaid = (double)request.TaxPaid,
                DeclaredInUK = request.DeclaredInUk ? 1 : 0
            });

        logger.LogInformation(
            "Tax entry {EntryId} added for user {UserId} (incomeType={IncomeType}, country={Country})",
            id, userId, request.IncomeType, LogSanitizer.Sanitize(request.Country));

        return new TaxEntryResponse(
            Id: Guid.Parse(id),
            IncomeType: request.IncomeType,
            Country: request.Country,
            TaxPaid: request.TaxPaid,
            DeclaredInUk: request.DeclaredInUk);
    }

    /// <inheritdoc/>
    public async Task<TaxEntryResponse?> UpdateAsync(
        string userId,
        Guid id,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE TaxEntries
               SET Country = @Country,
                   IncomeType = @IncomeType,
                   TaxPaid = @TaxPaid,
                   DeclaredInUK = @DeclaredInUK
             WHERE Id = @Id AND UserId = @UserId
            """,
            new
            {
                Id = id.ToString(),
                UserId = userId,
                request.Country,
                IncomeType = request.IncomeType.ToString(),
                TaxPaid = (double)request.TaxPaid,
                DeclaredInUK = request.DeclaredInUk ? 1 : 0
            });

        if (affected == 0)
        {
            logger.LogWarning(
                "Update requested for non-existent tax entry {EntryId} for user {UserId}", id, userId);
            return null;
        }

        logger.LogInformation("Tax entry {EntryId} updated for user {UserId}", id, userId);

        return new TaxEntryResponse(
            Id: id,
            IncomeType: request.IncomeType,
            Country: request.Country,
            TaxPaid: request.TaxPaid,
            DeclaredInUk: request.DeclaredInUk);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM TaxEntries WHERE Id = @Id AND UserId = @UserId",
            new { Id = id.ToString(), UserId = userId });

        if (affected > 0)
            logger.LogInformation("Tax entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent tax entry {EntryId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(TaxEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Country))
            throw new TaxValidationException("Country is required.");
        if (request.Country.Length > 100)
            throw new TaxValidationException("Country must not exceed 100 characters.");

        if (request.TaxPaid < 0)
            throw new TaxValidationException("Tax paid must be zero or greater.");
    }

    private static TaxEntryResponse MapToResponse(TaxEntryRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            IncomeType: Enum.Parse<IncomeType>(row.IncomeType, ignoreCase: true),
            Country: row.Country,
            TaxPaid: (decimal)row.TaxPaid,
            DeclaredInUk: row.DeclaredInUK != 0);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class TaxEntryRow
    {
        public string Id { get; init; } = "";
        public string Country { get; init; } = "";
        public string IncomeType { get; init; } = "";
        public double TaxPaid { get; init; }
        public int DeclaredInUK { get; init; }
    }
}
