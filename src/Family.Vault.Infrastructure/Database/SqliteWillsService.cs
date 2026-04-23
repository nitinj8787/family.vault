using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IWillsService"/>.
/// </summary>
public sealed class SqliteWillsService(
    FamilyVaultDbContext dbContext,
    ILogger<SqliteWillsService> logger) : IWillsService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<WillEntryResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<WillEntryRow>(
            "SELECT Id, Country, ExistsFlag, Location, ExecutorName FROM WillEntries WHERE UserId = @UserId",
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<WillEntryResponse> AddAsync(
        string userId,
        WillEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync(
            """
            INSERT INTO WillEntries (Id, UserId, Country, ExistsFlag, Location, ExecutorName, LastUpdated)
            VALUES (@Id, @UserId, @Country, @ExistsFlag, @Location, @ExecutorName, @LastUpdated)
            """,
            new
            {
                Id = id,
                UserId = userId,
                request.Country,
                ExistsFlag = request.WillExists ? 1 : 0,
                request.Location,
                ExecutorName = request.Executor,
                LastUpdated = DateTime.UtcNow.ToString("o")
            });

        logger.LogInformation(
            "Will entry {EntryId} added for user {UserId} (country={Country}, willExists={WillExists})",
            id, userId, LogSanitizer.Sanitize(request.Country), request.WillExists);

        return new WillEntryResponse(
            Id: Guid.Parse(id),
            Country: request.Country,
            WillExists: request.WillExists,
            Location: request.Location,
            Executor: request.Executor);
    }

    /// <inheritdoc/>
    public async Task<WillEntryResponse?> UpdateAsync(
        string userId,
        Guid id,
        WillEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var affected = await conn.ExecuteAsync(
            """
            UPDATE WillEntries
               SET Country = @Country,
                   ExistsFlag = @ExistsFlag,
                   Location = @Location,
                   ExecutorName = @ExecutorName,
                   LastUpdated = @LastUpdated
             WHERE Id = @Id AND UserId = @UserId
            """,
            new
            {
                Id = id.ToString(),
                UserId = userId,
                request.Country,
                ExistsFlag = request.WillExists ? 1 : 0,
                request.Location,
                ExecutorName = request.Executor,
                LastUpdated = DateTime.UtcNow.ToString("o")
            });

        if (affected == 0)
        {
            logger.LogWarning(
                "Update requested for non-existent will entry {EntryId} for user {UserId}", id, userId);
            return null;
        }

        logger.LogInformation("Will entry {EntryId} updated for user {UserId}", id, userId);

        return new WillEntryResponse(
            Id: id,
            Country: request.Country,
            WillExists: request.WillExists,
            Location: request.Location,
            Executor: request.Executor);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var affected = await conn.ExecuteAsync(
            "DELETE FROM WillEntries WHERE Id = @Id AND UserId = @UserId",
            new { Id = id.ToString(), UserId = userId });

        if (affected > 0)
            logger.LogInformation("Will entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent will entry {EntryId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(WillEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Country))
            throw new WillsValidationException("Country is required.");
        if (request.Country.Length > 100)
            throw new WillsValidationException("Country must not exceed 100 characters.");

        if (request.Location?.Length > 200)
            throw new WillsValidationException("Location must not exceed 200 characters.");

        if (request.Executor?.Length > 150)
            throw new WillsValidationException("Executor name must not exceed 150 characters.");
    }

    private static WillEntryResponse MapToResponse(WillEntryRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            Country: row.Country,
            WillExists: row.ExistsFlag != 0,
            Location: row.Location,
            Executor: row.ExecutorName);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class WillEntryRow
    {
        public string Id { get; init; } = "";
        public string Country { get; init; } = "";
        public int ExistsFlag { get; init; }
        public string? Location { get; init; }
        public string? ExecutorName { get; init; }
    }
}
