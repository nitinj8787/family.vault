using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages bank accounts for Family Vault users.
/// </summary>
public interface IBankAccountService
{
    /// <summary>Returns all bank accounts belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<BankAccountResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new bank account for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.BankAccountValidationException">
    /// Thrown when the request fails validation (including duplicate detection).
    /// </exception>
    Task<BankAccountResponse> AddAsync(
        string userId,
        BankAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the account identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated account, or <c>null</c> when the account was not found.</returns>
    /// <exception cref="Exceptions.BankAccountValidationException">
    /// Thrown when the request fails validation (including duplicate detection).
    /// </exception>
    Task<BankAccountResponse?> UpdateAsync(
        string userId,
        Guid id,
        BankAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the account identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the account was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
