using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages insurance policies for Family Vault users.
/// </summary>
public interface IInsuranceService
{
    /// <summary>Returns all insurance policies belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<InsuranceResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new insurance policy for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.InsuranceValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<InsuranceResponse> AddAsync(
        string userId,
        InsuranceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the insurance policy identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated policy, or <c>null</c> when the policy was not found.</returns>
    /// <exception cref="Exceptions.InsuranceValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<InsuranceResponse?> UpdateAsync(
        string userId,
        Guid id,
        InsuranceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the insurance policy identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the policy was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
