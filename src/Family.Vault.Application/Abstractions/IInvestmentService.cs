using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages investment holdings for Family Vault users.
/// </summary>
public interface IInvestmentService
{
    /// <summary>Returns all investments belonging to <paramref name="userId"/>.</summary>
    Task<IReadOnlyList<InvestmentResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new investment for <paramref name="userId"/> and returns the persisted record.
    /// </summary>
    /// <exception cref="Exceptions.InvestmentValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<InvestmentResponse> AddAsync(
        string userId,
        InvestmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the investment identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns>The updated investment, or <c>null</c> when the investment was not found.</returns>
    /// <exception cref="Exceptions.InvestmentValidationException">
    /// Thrown when the request fails validation.
    /// </exception>
    Task<InvestmentResponse?> UpdateAsync(
        string userId,
        Guid id,
        InvestmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the investment identified by <paramref name="id"/> for <paramref name="userId"/>.
    /// </summary>
    /// <returns><c>true</c> if the investment was found and deleted; <c>false</c> otherwise.</returns>
    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
