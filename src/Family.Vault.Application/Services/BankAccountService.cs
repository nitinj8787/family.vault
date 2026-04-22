using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IBankAccountService"/>.
/// Accounts are keyed by <c>(userId, accountId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class BankAccountService(ILogger<BankAccountService> logger) : IBankAccountService
{
    private readonly ConcurrentDictionary<(string UserId, Guid AccountId), BankAccount> _store =
        new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<BankAccountResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var accounts = _store.Values
            .Where(a => a.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<BankAccountResponse>>(accounts);
    }

    /// <inheritdoc/>
    public Task<BankAccountResponse> AddAsync(
        string userId,
        BankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        EnsureNoDuplicate(userId, excludeId: null, request);

        var id = Guid.NewGuid();
        var account = new BankAccount(id, userId, request.BankName, request.AccountType,
            request.AccountNumber, request.Nominee);

        _store[(userId, id)] = account;

        logger.LogInformation(
            "Bank account {AccountId} added for user {UserId} (bankName={BankName}, accountType={AccountType})",
            id, userId, LogSanitizer.Sanitize(request.BankName), request.AccountType);

        return Task.FromResult(MapToResponse(account));
    }

    /// <inheritdoc/>
    public Task<BankAccountResponse?> UpdateAsync(
        string userId,
        Guid id,
        BankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_store.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent bank account {AccountId} for user {UserId}", id, userId);
            return Task.FromResult<BankAccountResponse?>(null);
        }

        EnsureNoDuplicate(userId, excludeId: id, request);

        var updated = new BankAccount(id, userId, request.BankName, request.AccountType,
            request.AccountNumber, request.Nominee);

        _store[(userId, id)] = updated;

        logger.LogInformation(
            "Bank account {AccountId} updated for user {UserId}", id, userId);

        return Task.FromResult<BankAccountResponse?>(MapToResponse(updated));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Bank account {AccountId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent bank account {AccountId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(BankAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankName))
            throw new BankAccountValidationException("Bank name is required.");

        if (request.BankName.Length > 150)
            throw new BankAccountValidationException("Bank name must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new BankAccountValidationException("Account number is required.");

        if (request.AccountNumber.Length > 50)
            throw new BankAccountValidationException("Account number must not exceed 50 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new BankAccountValidationException("Nominee name must not exceed 100 characters.");
    }

    /// <summary>
    /// Ensures that no other account for <paramref name="userId"/> already has the same
    /// bank name and account number combination (case-insensitive), optionally
    /// excluding the account being updated (<paramref name="excludeId"/>).
    /// </summary>
    private void EnsureNoDuplicate(string userId, Guid? excludeId, BankAccountRequest request)
    {
        var duplicate = _store.Values.Any(a =>
            a.UserId == userId &&
            (excludeId is null || a.Id != excludeId) &&
            string.Equals(a.BankName, request.BankName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.AccountNumber, request.AccountNumber, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
            throw new BankAccountValidationException(
                $"An account with the same bank name and account number already exists.");
    }

    /// <summary>
    /// Maps a domain entity to a response DTO, masking all but the last four
    /// characters of the account number.
    /// </summary>
    private static BankAccountResponse MapToResponse(BankAccount account)
    {
        var masked = MaskAccountNumber(account.AccountNumber);
        return new BankAccountResponse(
            Id: account.Id,
            BankName: account.BankName,
            AccountType: account.AccountType,
            MaskedAccountNumber: masked,
            Nominee: account.Nominee);
    }

    /// <summary>
    /// Returns the account number with all but the last four characters replaced by '•'.
    /// Short values (four characters or fewer) are returned fully masked.
    /// </summary>
    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return new string('•', accountNumber.Length);

        var visible = accountNumber[^4..];
        return new string('•', accountNumber.Length - 4) + visible;
    }
}
