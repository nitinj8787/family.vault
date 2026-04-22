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
/// In-memory implementation of <see cref="IIndiaAssetService"/>.
/// Assets are keyed by <c>(userId, assetId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class IndiaAssetService(ILogger<IndiaAssetService> logger) : IIndiaAssetService
{
    private readonly ConcurrentDictionary<(string UserId, Guid AssetId), IndiaAsset> _store =
        new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<IndiaAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var assets = _store.Values
            .Where(a => a.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<IndiaAssetResponse>>(assets);
    }

    /// <inheritdoc/>
    public Task<IndiaAssetResponse> AddAsync(
        string userId,
        IndiaAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var asset = new IndiaAsset(id, userId, request.Category, request.BankOrPlatform,
            request.AccountType, request.Repatriation, request.Nominee);

        _store[(userId, id)] = asset;

        logger.LogInformation(
            "India asset {AssetId} added for user {UserId} (category={Category}, platform={Platform}, accountType={AccountType})",
            id, userId, request.Category, LogSanitizer.Sanitize(request.BankOrPlatform), request.AccountType);

        return Task.FromResult(MapToResponse(asset));
    }

    /// <inheritdoc/>
    public Task<IndiaAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        IndiaAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent India asset {AssetId} for user {UserId}", id, userId);
            return Task.FromResult<IndiaAssetResponse?>(null);
        }

        var updated = new IndiaAsset(id, userId, request.Category, request.BankOrPlatform,
            request.AccountType, request.Repatriation, request.Nominee);

        _store[(userId, id)] = updated;

        logger.LogInformation(
            "India asset {AssetId} updated for user {UserId}", id, userId);

        return Task.FromResult<IndiaAssetResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("India asset {AssetId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent India asset {AssetId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(IndiaAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankOrPlatform))
            throw new IndiaAssetValidationException("Bank / platform is required.");

        if (request.BankOrPlatform.Length > 150)
            throw new IndiaAssetValidationException("Bank / platform must not exceed 150 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new IndiaAssetValidationException("Nominee name must not exceed 100 characters.");

        // Business rule: repatriation status must be consistent with account type.
        if (request.AccountType == NriAccountType.NRE &&
            request.Repatriation != RepatriationStatus.FullyRepatriable)
        {
            throw new IndiaAssetValidationException(
                "NRE accounts must be marked as Fully Repatriable.");
        }

        if (request.AccountType == NriAccountType.NRO &&
            request.Repatriation != RepatriationStatus.Limited)
        {
            throw new IndiaAssetValidationException(
                "NRO accounts must be marked as Limited repatriation.");
        }
    }

    /// <summary>
    /// Maps a domain entity to a response DTO, computing advisory flags
    /// (<see cref="IndiaAssetResponse.IsTaxableInIndia"/>, <see cref="IndiaAssetResponse.IsRepatriable"/>)
    /// from the account type.
    /// </summary>
    private static IndiaAssetResponse MapToResponse(IndiaAsset asset) =>
        new(
            Id: asset.Id,
            Category: asset.Category,
            BankOrPlatform: asset.BankOrPlatform,
            AccountType: asset.AccountType,
            Repatriation: asset.Repatriation,
            Nominee: asset.Nominee,
            IsTaxableInIndia: asset.AccountType == NriAccountType.NRO,
            IsRepatriable: asset.AccountType == NriAccountType.NRE);
}
