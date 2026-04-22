using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IWillsService"/>.
/// Entries are keyed by <c>(userId, entryId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class WillsService(ILogger<WillsService> logger) : IWillsService
{
    private readonly ConcurrentDictionary<(string UserId, Guid EntryId), WillEntry> _store =
        new ConcurrentDictionary<(string UserId, Guid EntryId), WillEntry>();

    /// <inheritdoc/>
    public Task<IReadOnlyList<WillEntryResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var entries = _store.Values
            .Where(e => e.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<WillEntryResponse>>(entries);
    }

    /// <inheritdoc/>
    public Task<WillEntryResponse> AddAsync(
        string userId,
        WillEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var entry = new WillEntry(
            id,
            userId,
            request.Country,
            request.WillExists,
            request.Location,
            request.Executor);

        _store[(userId, id)] = entry;

        logger.LogInformation(
            "Will entry {EntryId} added for user {UserId} (country={Country}, willExists={WillExists})",
            id, userId, LogSanitizer.Sanitize(request.Country), request.WillExists);

        return Task.FromResult(MapToResponse(entry));
    }

    /// <inheritdoc/>
    public Task<WillEntryResponse?> UpdateAsync(
        string userId,
        Guid id,
        WillEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent will entry {EntryId} for user {UserId}", id, userId);
            return Task.FromResult<WillEntryResponse?>(null);
        }

        var updated = new WillEntry(
            id,
            userId,
            request.Country,
            request.WillExists,
            request.Location,
            request.Executor);

        _store[(userId, id)] = updated;

        logger.LogInformation("Will entry {EntryId} updated for user {UserId}", id, userId);
        return Task.FromResult<WillEntryResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Will entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent will entry {EntryId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
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

    private static WillEntryResponse MapToResponse(WillEntry entry) =>
        new(
            Id: entry.Id,
            Country: entry.Country,
            WillExists: entry.WillExists,
            Location: entry.Location,
            Executor: entry.Executor);
}
