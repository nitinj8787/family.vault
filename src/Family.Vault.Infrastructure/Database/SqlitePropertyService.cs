using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IPropertyService"/>.
/// Each property is stored as a row in <c>Assets</c> (AssetType = 'Property')
/// joined with a row in <c>Properties</c>.
/// AssetName is stored in <c>Assets.Name</c> and country in <c>Assets.Country</c>.
/// </summary>
public sealed class SqlitePropertyService(
    FamilyVaultDbContext dbContext,
    ILogger<SqlitePropertyService> logger) : IPropertyService
{
    private const string AssetType = "Property";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PropertyResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<PropertyRow>(
            """
            SELECT a.Id, a.Name AS AssetName, a.Country, p.OwnershipType, p.LoanLinked, p.DocumentsLocation
              FROM Properties p
              JOIN Assets a ON p.Id = a.Id
             WHERE a.UserId = @UserId AND a.IsActive = 1
            """,
            new { UserId = userId });

        return rows.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public async Task<PropertyResponse> AddAsync(
        string userId,
        PropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid().ToString();

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
                using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO Assets (Id, UserId, AssetType, Name, Country, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @AssetType, @Name, @Country, 1, @CreatedAt)
            """,
            new
            {
                Id = id,
                UserId = userId,
                AssetType,
                Name = request.AssetName,
                Country = request.Country.ToString(),
                CreatedAt = DateTime.UtcNow.ToString("o")
            },
            tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO Properties (Id, OwnershipType, LoanLinked, DocumentsLocation)
            VALUES (@Id, @OwnershipType, @LoanLinked, @DocumentsLocation)
            """,
            new
            {
                Id = id,
                OwnershipType = request.Ownership,
                LoanLinked = request.LoanLinked ? 1 : 0,
                request.DocumentsLocation
            },
            tx);

        tx.Commit();

        logger.LogInformation(
            "Property {PropertyId} added for user {UserId} (assetName={AssetName}, country={Country}, loanLinked={LoanLinked})",
            id, userId, LogSanitizer.Sanitize(request.AssetName), request.Country, request.LoanLinked);

        return new PropertyResponse(
            Id: Guid.Parse(id),
            AssetName: request.AssetName,
            Country: request.Country,
            Ownership: request.Ownership,
            LoanLinked: request.LoanLinked,
            DocumentsLocation: request.DocumentsLocation);
    }

    /// <inheritdoc/>
    public async Task<PropertyResponse?> UpdateAsync(
        string userId,
        Guid id,
        PropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Assets WHERE Id = @Id AND UserId = @UserId AND AssetType = @AssetType",
            new { Id = id.ToString(), UserId = userId, AssetType });

        if (exists == 0)
        {
            logger.LogWarning("Update requested for non-existent property {PropertyId} for user {UserId}", id, userId);
            return null;
        }

        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE Assets SET Name = @Name, Country = @Country WHERE Id = @Id AND UserId = @UserId",
            new { Name = request.AssetName, Country = request.Country.ToString(), Id = id.ToString(), UserId = userId },
            tx);

        await conn.ExecuteAsync(
            """
            UPDATE Properties
               SET OwnershipType = @OwnershipType,
                   LoanLinked = @LoanLinked,
                   DocumentsLocation = @DocumentsLocation
             WHERE Id = @Id
            """,
            new
            {
                OwnershipType = request.Ownership,
                LoanLinked = request.LoanLinked ? 1 : 0,
                request.DocumentsLocation,
                Id = id.ToString()
            },
            tx);

        tx.Commit();

        logger.LogInformation("Property {PropertyId} updated for user {UserId}", id, userId);

        return new PropertyResponse(
            Id: id,
            AssetName: request.AssetName,
            Country: request.Country,
            Ownership: request.Ownership,
            LoanLinked: request.LoanLinked,
            DocumentsLocation: request.DocumentsLocation);
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
            logger.LogInformation("Property {PropertyId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent property {PropertyId} for user {UserId}", id, userId);

        return affected > 0;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(PropertyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AssetName))
            throw new PropertyValidationException("Asset name is required.");
        if (request.AssetName.Length > 200)
            throw new PropertyValidationException("Asset name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(request.Ownership))
            throw new PropertyValidationException("Ownership is required.");
        if (request.Ownership.Length > 100)
            throw new PropertyValidationException("Ownership must not exceed 100 characters.");

        if (request.DocumentsLocation is { Length: > 300 })
            throw new PropertyValidationException("Documents location must not exceed 300 characters.");
    }

    private static PropertyResponse MapToResponse(PropertyRow row) =>
        new(
            Id: Guid.Parse(row.Id),
            AssetName: row.AssetName,
            Country: Enum.Parse<PropertyCountry>(row.Country, ignoreCase: true),
            Ownership: row.OwnershipType,
            LoanLinked: row.LoanLinked != 0,
            DocumentsLocation: row.DocumentsLocation);

    // -----------------------------------------------------------------
    // Row mapping class
    // -----------------------------------------------------------------

    private sealed class PropertyRow
    {
        public string Id { get; init; } = "";
        public string AssetName { get; init; } = "";
        public string Country { get; init; } = "";
        public string OwnershipType { get; init; } = "";
        public int LoanLinked { get; init; }
        public string? DocumentsLocation { get; init; }
    }
}
