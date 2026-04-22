using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="INomineeService"/>.
/// </summary>
public sealed class SqliteNomineeService(
    SqliteConnectionFactory connectionFactory,
    ILogger<SqliteNomineeService> logger) : INomineeService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<NomineeResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<NomineeRow>(
            "SELECT Id, AssetType, ContactDetails, Name, Relationship FROM Nominees WHERE UserId = @UserId",
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<NomineeResponse> AddAsync(
        string userId,
        NomineeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        using var conn = connectionFactory.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO Nominees (Id, UserId, AssetType, ContactDetails, Name, Relationship)
            VALUES (@Id, @UserId, @AssetType, @ContactDetails, @Name, @Relationship)
            """,
            new
            {
                Id = id,
                UserId = userId,
                AssetType = request.AssetType.ToString(),
                ContactDetails = request.Institution,
                Name = request.NomineeName,
                request.Relationship
            });

        logger.LogInformation(
            "Nominee entry {NomineeId} added for user {UserId} (assetType={AssetType}, institution={Institution})",
            id, userId, request.AssetType, LogSanitizer.Sanitize(request.Institution));

        return new NomineeResponse(
            Id: Guid.Parse(id),
            AssetType: request.AssetType,
            Institution: request.Institution,
            NomineeName: request.NomineeName,
            Relationship: request.Relationship);
    }

    /// <inheritdoc/>
    public async Task<NomineeResponse?> UpdateAsync(
        string userId,
        Guid id,
        NomineeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE Nominees
               SET AssetType = @AssetType,
                   ContactDetails = @ContactDetails,
                   Name = @Name,
                   Relationship = @Relationship
             WHERE Id = @Id AND UserId = @UserId
            """,
            new
            {
                Id = id.ToString(),
                UserId = userId,
                AssetType = request.AssetType.ToString(),
                ContactDetails = request.Institution,
                Name = request.NomineeName,
                request.Relationship
            });

        if (affected == 0)
        {
            logger.LogWarning(
                "Update requested for non-existent nominee entry {NomineeId} for user {UserId}", id, userId);
            return null;
        }

        logger.LogInformation("Nominee entry {NomineeId} updated for user {UserId}", id, userId);

        return new NomineeResponse(
            Id: id,
            AssetType: request.AssetType,
            Institution: request.Institution,
            NomineeName: request.NomineeName,
            Relationship: request.Relationship);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM Nominees WHERE Id = @Id AND UserId = @UserId",
            new { Id = id.ToString(), UserId = userId });

        if (affected > 0)
            logger.LogInformation("Nominee entry {NomineeId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent nominee entry {NomineeId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(NomineeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Institution))
            throw new NomineeValidationException("Institution is required.");
        if (request.Institution.Length > 150)
            throw new NomineeValidationException("Institution must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.NomineeName))
            throw new NomineeValidationException("Nominee name is required.");
        if (request.NomineeName.Length > 150)
            throw new NomineeValidationException("Nominee name must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.Relationship))
            throw new NomineeValidationException("Relationship is required.");
        if (request.Relationship.Length > 100)
            throw new NomineeValidationException("Relationship must not exceed 100 characters.");
    }

    private static NomineeResponse MapToResponse(NomineeRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            AssetType: Enum.Parse<NomineeAssetType>(row.AssetType, ignoreCase: true),
            Institution: row.ContactDetails ?? "",
            NomineeName: row.Name,
            Relationship: row.Relationship ?? "");

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class NomineeRow
    {
        public string Id { get; init; } = "";
        public string AssetType { get; init; } = "";
        public string? ContactDetails { get; init; }
        public string Name { get; init; } = "";
        public string? Relationship { get; init; }
    }
}
