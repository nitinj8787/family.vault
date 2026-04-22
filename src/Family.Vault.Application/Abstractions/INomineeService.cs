using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages nominee entries for Family Vault users.
/// </summary>
public interface INomineeService
{
    /// <summary>Returns all nominee entries belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<NomineeResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new nominee entry for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.NomineeValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<NomineeResponse> AddAsync(
        string userId,
        NomineeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the nominee entry identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated entry, or <c>null</c> when the entry was not found.</returns>
    /// <exception cref="Exceptions.NomineeValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<NomineeResponse?> UpdateAsync(
        string userId,
        Guid id,
        NomineeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the nominee entry identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the entry was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
