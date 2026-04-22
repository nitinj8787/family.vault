using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages property and loan assets for Family Vault users.
/// </summary>
public interface IPropertyService
{
    /// <summary>Returns all properties belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<PropertyResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new property for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.PropertyValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<PropertyResponse> AddAsync(
        string userId,
        PropertyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the property identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated property, or <c>null</c> when the property was not found.</returns>
    /// <exception cref="Exceptions.PropertyValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<PropertyResponse?> UpdateAsync(
        string userId,
        Guid id,
        PropertyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the property identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the property was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
