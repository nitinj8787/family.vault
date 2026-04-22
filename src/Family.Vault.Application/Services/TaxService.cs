using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="ITaxService"/>.
/// Entries are keyed by <c>(userId, entryId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class TaxService(ILogger<TaxService> logger) : ITaxService
{
    private readonly ConcurrentDictionary<(string UserId, Guid EntryId), TaxEntry> _store =
        new ConcurrentDictionary<(string UserId, Guid EntryId), TaxEntry>();

    /// <inheritdoc/>
    public Task<IReadOnlyList<TaxEntryResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var entries = _store.Values
            .Where(e => e.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<TaxEntryResponse>>(entries);
    }

    /// <inheritdoc/>
    public Task<TaxEntryResponse> AddAsync(
        string userId,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var entry = new TaxEntry(
            id,
            userId,
            request.IncomeType,
            request.Country,
            request.TaxPaid,
            request.DeclaredInUk);

        _store[(userId, id)] = entry;

        logger.LogInformation(
            "Tax entry {EntryId} added for user {UserId} (incomeType={IncomeType}, country={Country})",
            id, userId, request.IncomeType, LogSanitizer.Sanitize(request.Country));

        return Task.FromResult(MapToResponse(entry));
    }

    /// <inheritdoc/>
    public Task<TaxEntryResponse?> UpdateAsync(
        string userId,
        Guid id,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent tax entry {EntryId} for user {UserId}", id, userId);
            return Task.FromResult<TaxEntryResponse?>(null);
        }

        var updated = new TaxEntry(
            id,
            userId,
            request.IncomeType,
            request.Country,
            request.TaxPaid,
            request.DeclaredInUk);

        _store[(userId, id)] = updated;

        logger.LogInformation("Tax entry {EntryId} updated for user {UserId}", id, userId);
        return Task.FromResult<TaxEntryResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Tax entry {EntryId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning(
                "Delete requested for non-existent tax entry {EntryId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
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

    private static TaxEntryResponse MapToResponse(TaxEntry entry) =>
        new(
            Id: entry.Id,
            IncomeType: entry.IncomeType,
            Country: entry.Country,
            TaxPaid: entry.TaxPaid,
            DeclaredInUk: entry.DeclaredInUk);
}
