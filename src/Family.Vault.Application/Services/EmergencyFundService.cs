using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IEmergencyFundService"/>.
/// Entries are keyed by <c>(userId, entryId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class EmergencyFundService(ILogger<EmergencyFundService> logger) : IEmergencyFundService
{
    private readonly ConcurrentDictionary<(string UserId, Guid EntryId), EmergencyFund> _store =
        new ConcurrentDictionary<(string UserId, Guid EntryId), EmergencyFund>();

    /// <inheritdoc/>
    public Task<IReadOnlyList<EmergencyFundResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var entries = _store.Values
            .Where(e => e.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<EmergencyFundResponse>>(entries);
    }

    /// <inheritdoc/>
    public Task<EmergencyFundResponse> AddAsync(
        string userId,
        EmergencyFundRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var entry = new EmergencyFund(id, userId, request.Location, request.Amount, request.AccessInstructions);

        _store[(userId, id)] = entry;

        logger.LogInformation(
            "Emergency fund entry {EntryId} added for user {UserId} (location={SanitizedLocation}, amount={Amount})",
            id, userId, LogSanitizer.Sanitize(request.Location), request.Amount);

        return Task.FromResult(MapToResponse(entry));
    }

    /// <inheritdoc/>
    public Task<EmergencyFundResponse?> UpdateAsync(
        string userId,
        Guid id,
        EmergencyFundRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent emergency fund entry {EntryId} for user {UserId}", id, userId);
            return Task.FromResult<EmergencyFundResponse?>(null);
        }

        var updated = new EmergencyFund(id, userId, request.Location, request.Amount, request.AccessInstructions);
        _store[(userId, id)] = updated;

        logger.LogInformation("Emergency fund entry {EntryId} updated for user {UserId}", id, userId);
        return Task.FromResult<EmergencyFundResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Emergency fund entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent emergency fund entry {EntryId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
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

    private static EmergencyFundResponse MapToResponse(EmergencyFund entry) =>
        new(
            Id: entry.Id,
            Location: entry.Location,
            Amount: entry.Amount,
            AccessInstructions: entry.AccessInstructions);
}
