using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IPropertyService"/>.
/// Properties are keyed by <c>(userId, propertyId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class PropertyService(ILogger<PropertyService> logger) : IPropertyService
{
    private readonly ConcurrentDictionary<(string UserId, Guid PropertyId), Property> _propertyStore =
        new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<PropertyResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var properties = _propertyStore.Values
            .Where(p => p.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<PropertyResponse>>(properties);
    }

    /// <inheritdoc/>
    public Task<PropertyResponse> AddAsync(
        string userId,
        PropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var property = new Property(
            id, userId, request.AssetName, request.Country,
            request.Ownership, request.LoanLinked, request.DocumentsLocation);

        _propertyStore[(userId, id)] = property;

        logger.LogInformation(
            "Property {PropertyId} added for user {UserId} (assetName={SanitizedAssetName}, country={Country}, loanLinked={LoanLinked})",
            id, userId, LogSanitizer.Sanitize(request.AssetName), request.Country, request.LoanLinked);

        return Task.FromResult(MapToResponse(property));
    }

    /// <inheritdoc/>
    public Task<PropertyResponse?> UpdateAsync(
        string userId,
        Guid id,
        PropertyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_propertyStore.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent property {PropertyId} for user {UserId}", id, userId);
            return Task.FromResult<PropertyResponse?>(null);
        }

        var updated = new Property(
            id, userId, request.AssetName, request.Country,
            request.Ownership, request.LoanLinked, request.DocumentsLocation);

        _propertyStore[(userId, id)] = updated;

        logger.LogInformation("Property {PropertyId} updated for user {UserId}", id, userId);
        return Task.FromResult<PropertyResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _propertyStore.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Property {PropertyId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent property {PropertyId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
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

    private static PropertyResponse MapToResponse(Property property) =>
        new(
            Id: property.Id,
            AssetName: property.AssetName,
            Country: property.Country,
            Ownership: property.Ownership,
            LoanLinked: property.LoanLinked,
            DocumentsLocation: property.DocumentsLocation);
}
