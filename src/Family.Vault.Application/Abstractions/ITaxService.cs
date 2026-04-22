using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages tax-summary entries for Family Vault users.
/// </summary>
public interface ITaxService
{
    /// <summary>Returns all tax-summary entries belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<TaxEntryResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tax-summary entry for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.TaxValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<TaxEntryResponse> AddAsync(
        string userId,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the entry identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated entry, or <c>null</c> when the entry was not found.</returns>
    /// <exception cref="Exceptions.TaxValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<TaxEntryResponse?> UpdateAsync(
        string userId,
        Guid id,
        TaxEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entry identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the entry was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
