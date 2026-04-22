using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages insurance policies for Family Vault users.
/// </summary>
public interface IInsuranceService
{
    Task<IReadOnlyList<InsuranceResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<InsuranceResponse> AddAsync(
        string userId,
        InsuranceRequest request,
        CancellationToken cancellationToken = default);

    Task<InsuranceResponse?> UpdateAsync(
        string userId,
        Guid id,
        InsuranceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);
}
