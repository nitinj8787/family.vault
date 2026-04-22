using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages India financial assets for Family Vault NRI users.
/// </summary>
public interface IIndiaAssetService
{
    /// <summary>Returns all India assets belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<IndiaAssetResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new India asset for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.IndiaAssetValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<IndiaAssetResponse> AddAsync(
        string userId,
        IndiaAssetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the asset identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated asset, or <c>null</c> when the asset was not found.</returns>
    /// <exception cref="Exceptions.IndiaAssetValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<IndiaAssetResponse?> UpdateAsync(
        string userId,
        Guid id,
        IndiaAssetRequest request,
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
