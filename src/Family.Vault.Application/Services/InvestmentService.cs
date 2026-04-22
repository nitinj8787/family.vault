using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IInvestmentService"/>.
/// Investments are keyed by <c>(userId, investmentId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class InvestmentService(ILogger<InvestmentService> logger) : IInvestmentService
{
    private readonly ConcurrentDictionary<(string UserId, Guid InvestmentId), Investment> _store =
        new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<InvestmentResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var investments = _store.Values
            .Where(i => i.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<InvestmentResponse>>(investments);
    }

    /// <inheritdoc/>
    public Task<InvestmentResponse> AddAsync(
        string userId,
        InvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var investment = new Investment(id, userId, request.Platform, request.Type,
            request.AccountId, request.Nominee);

        _store[(userId, id)] = investment;

        logger.LogInformation(
            "Investment {InvestmentId} added for user {UserId} (platform={Platform}, type={Type})",
            id, userId, LogSanitizer.Sanitize(request.Platform), request.Type);

        return Task.FromResult(MapToResponse(investment));
    }

    /// <inheritdoc/>
    public Task<InvestmentResponse?> UpdateAsync(
        string userId,
        Guid id,
        InvestmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent investment {InvestmentId} for user {UserId}", id, userId);
            return Task.FromResult<InvestmentResponse?>(null);
        }

        var updated = new Investment(id, userId, request.Platform, request.Type,
            request.AccountId, request.Nominee);

        _store[(userId, id)] = updated;

        logger.LogInformation(
            "Investment {InvestmentId} updated for user {UserId}", id, userId);

        return Task.FromResult<InvestmentResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Investment {InvestmentId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent investment {InvestmentId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(InvestmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform))
            throw new InvestmentValidationException("Platform is required.");

        if (request.Platform.Length > 150)
            throw new InvestmentValidationException("Platform must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountId))
            throw new InvestmentValidationException("Account ID is required.");

        if (request.AccountId.Length > 50)
            throw new InvestmentValidationException("Account ID must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new InvestmentValidationException("Nominee name must not exceed 100 characters.");
    }

    /// <summary>
    /// Maps a domain entity to a response DTO, masking all but the last four
    /// characters of the account / folio ID.
    /// </summary>
    private static InvestmentResponse MapToResponse(Investment investment)
    {
        var masked = MaskAccountId(investment.AccountId);
        return new InvestmentResponse(
            Id: investment.Id,
            Platform: investment.Platform,
            Type: investment.Type,
            MaskedAccountId: masked,
            Nominee: investment.Nominee);
    }

    /// <summary>
    /// Returns the account ID with all but the last four characters replaced by '•'.
    /// Short values (four characters or fewer) are returned fully masked.
    /// </summary>
    private static string MaskAccountId(string accountId)
    {
        if (accountId.Length <= 4)
            return new string('•', accountId.Length);

        var visible = accountId[^4..];
        return new string('•', accountId.Length - 4) + visible;
    }
}
