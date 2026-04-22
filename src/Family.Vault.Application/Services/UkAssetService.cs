using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Family.Vault.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IUkAssetService"/>.
/// Assets are keyed by <c>(userId, assetId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class UkAssetService(ILogger<UkAssetService> logger) : IUkAssetService
{
    // Key: (userId, assetId) – allows O(1) lookup and safe per-user isolation.
    private readonly ConcurrentDictionary<(string UserId, Guid AssetId), UkAsset> _store =
        new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<UkAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var assets = _store.Values
            .Where(a => a.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<UkAssetResponse>>(assets);
    }

    /// <inheritdoc/>
    public Task<UkAssetResponse> AddAsync(
        string userId,
        UkAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var asset = new UkAsset(id, userId, request.Category, request.Provider,
            request.AccountNumber, request.Nominee, request.TaxNotes);

        _store[(userId, id)] = asset;

        logger.LogInformation(
            "UK asset {AssetId} added for user {UserId} (category={Category}, provider={Provider})",
            id, userId, request.Category, LogSanitizer.Sanitize(request.Provider));

        return Task.FromResult(MapToResponse(asset));
    }

    /// <inheritdoc/>
    public Task<UkAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        UkAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent asset {AssetId} for user {UserId}", id, userId);
            return Task.FromResult<UkAssetResponse?>(null);
        }

        var updated = new UkAsset(id, userId, request.Category, request.Provider,
            request.AccountNumber, request.Nominee, request.TaxNotes);

        _store[(userId, id)] = updated;

        logger.LogInformation(
            "UK asset {AssetId} updated for user {UserId}", id, userId);

        return Task.FromResult<UkAssetResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("UK asset {AssetId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent asset {AssetId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(UkAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new UkAssetValidationException("Provider is required.");

        if (request.Provider.Length > 150)
            throw new UkAssetValidationException("Provider must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new UkAssetValidationException("Account number is required.");

        if (request.AccountNumber.Length > 50)
            throw new UkAssetValidationException("Account number must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new UkAssetValidationException("Nominee name must not exceed 100 characters.");

        if (request.TaxNotes is { Length: > 500 })
            throw new UkAssetValidationException("Tax notes must not exceed 500 characters.");
    }

    /// <summary>
    /// Maps a domain entity to a response DTO, masking all but the last four
    /// characters of the account number.
    /// </summary>
    private static UkAssetResponse MapToResponse(UkAsset asset)
    {
        var masked = MaskAccountNumber(asset.AccountNumber);
        return new UkAssetResponse(
            Id: asset.Id,
            Category: asset.Category,
            Provider: asset.Provider,
            MaskedAccountNumber: masked,
            Nominee: asset.Nominee,
            TaxNotes: asset.TaxNotes);
    }

    /// <summary>
    /// Returns the account number with all but the last four characters replaced by '•'.
    /// Short values (four characters or fewer) are returned fully masked as '••••'.
    /// </summary>
    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return new string('•', accountNumber.Length);

        var visible = accountNumber[^4..];
        return new string('•', accountNumber.Length - 4) + visible;
    }
}
