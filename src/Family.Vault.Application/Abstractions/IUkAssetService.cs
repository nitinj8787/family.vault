using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages UK financial assets for Family Vault users.
/// </summary>
public interface IUkAssetService
{
    /// <summary>Returns all assets belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<UkAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new asset for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.UkAssetValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<UkAssetResponse> AddAsync(
        string userId,
        UkAssetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the asset identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated asset, or <c>null</c> when the asset was not found.</returns>
    /// <exception cref="Exceptions.UkAssetValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<UkAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        UkAssetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the asset identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the asset was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
