using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IEmergencyFundService"/>.
/// </summary>
public sealed class SqliteEmergencyFundService(
    SqliteConnectionFactory connectionFactory,
    ILogger<SqliteEmergencyFundService> logger) : IEmergencyFundService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmergencyFundResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<EmergencyFundRow>(
            "SELECT Id, Location, Amount, AccessInstructions FROM EmergencyFunds WHERE UserId = @UserId",
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<EmergencyFundResponse> AddAsync(
        string userId,
        EmergencyFundRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO EmergencyFunds (Id, UserId, Location, Amount, AccessInstructions)
            VALUES (@Id, @UserId, @Location, @Amount, @AccessInstructions)
            """,
            new
            {
                Id = id,
                UserId = userId,
                request.Location,
                Amount = (double)request.Amount,
                request.AccessInstructions
            });

        logger.LogInformation(
            "Emergency fund entry {EntryId} added for user {UserId} (location={Location}, amount={Amount})",
            id, userId, LogSanitizer.Sanitize(request.Location), request.Amount);

        return new EmergencyFundResponse(
            Id: Guid.Parse(id),
            Location: request.Location,
            Amount: request.Amount,
            AccessInstructions: request.AccessInstructions);
    }

    /// <inheritdoc/>
    public async Task<EmergencyFundResponse?> UpdateAsync(
        string userId,
        Guid id,
        EmergencyFundRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE EmergencyFunds
               SET Location = @Location,
                   Amount = @Amount,
                   AccessInstructions = @AccessInstructions
             WHERE Id = @Id AND UserId = @UserId
            """,
            new
            {
                Id = id.ToString(),
                UserId = userId,
                request.Location,
                Amount = (double)request.Amount,
                request.AccessInstructions
            });

        if (affected == 0)
        {
            logger.LogWarning(
                "Update requested for non-existent emergency fund entry {EntryId} for user {UserId}", id, userId);
            return null;
        }

        logger.LogInformation("Emergency fund entry {EntryId} updated for user {UserId}", id, userId);

        return new EmergencyFundResponse(
            Id: id,
            Location: request.Location,
            Amount: request.Amount,
            AccessInstructions: request.AccessInstructions);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM EmergencyFunds WHERE Id = @Id AND UserId = @UserId",
            new { Id = id.ToString(), UserId = userId });

        if (affected > 0)
            logger.LogInformation("Emergency fund entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent emergency fund entry {EntryId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(EmergencyFundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Location))
            throw new EmergencyFundValidationException("Location is required.");
        if (request.Location.Length > 150)
            throw new EmergencyFundValidationException("Location must not exceed 150 characters.");

        if (request.Amount < 0)
            throw new EmergencyFundValidationException("Amount must be zero or greater.");

        if (request.AccessInstructions is { Length: > 500 })
            throw new EmergencyFundValidationException("Access instructions must not exceed 500 characters.");
    }

    private static EmergencyFundResponse MapToResponse(EmergencyFundRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            Location: row.Location,
            Amount: (decimal)row.Amount,
            AccessInstructions: row.AccessInstructions);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class EmergencyFundRow
    {
        public string Id { get; init; } = "";
        public string Location { get; init; } = "";
        public double Amount { get; init; }
        public string? AccessInstructions { get; init; }
    }
}
