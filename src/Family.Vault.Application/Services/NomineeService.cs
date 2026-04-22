using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="INomineeService"/>.
/// Entries are keyed by <c>(userId, nomineeId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class NomineeService(ILogger<NomineeService> logger) : INomineeService
{
    private readonly ConcurrentDictionary<(string UserId, Guid NomineeId), Nominee> _store =
        new ConcurrentDictionary<(string UserId, Guid NomineeId), Nominee>();

    /// <inheritdoc/>
    public Task<IReadOnlyList<NomineeResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var entries = _store.Values
            .Where(n => n.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<NomineeResponse>>(entries);
    }

    /// <inheritdoc/>
    public Task<NomineeResponse> AddAsync(
        string userId,
        NomineeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var nominee = new Nominee(
            id,
            userId,
            request.AssetType,
            request.Institution,
            request.NomineeName,
            request.Relationship);

        _store[(userId, id)] = nominee;

        logger.LogInformation(
            "Nominee entry {NomineeId} added for user {UserId} (assetType={AssetType}, institution={Institution})",
            id, userId, request.AssetType, LogSanitizer.Sanitize(request.Institution));

        return Task.FromResult(MapToResponse(nominee));
    }

    /// <inheritdoc/>
    public Task<NomineeResponse?> UpdateAsync(
        string userId,
        Guid id,
        NomineeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent nominee entry {NomineeId} for user {UserId}", id, userId);
            return Task.FromResult<NomineeResponse?>(null);
        }

        var updated = new Nominee(
            id,
            userId,
            request.AssetType,
            request.Institution,
            request.NomineeName,
            request.Relationship);

        _store[(userId, id)] = updated;

        logger.LogInformation("Nominee entry {NomineeId} updated for user {UserId}", id, userId);
        return Task.FromResult<NomineeResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Nominee entry {NomineeId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent nominee entry {NomineeId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
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

    private static NomineeResponse MapToResponse(Nominee nominee) =>
        new(
            Id: nominee.Id,
            AssetType: nominee.AssetType,
            Institution: nominee.Institution,
            NomineeName: nominee.NomineeName,
            Relationship: nominee.Relationship);
}
