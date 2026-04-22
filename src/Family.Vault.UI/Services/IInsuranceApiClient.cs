using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing insurance policies via the FamilyVault API.
/// </summary>
public interface IInsuranceApiClient
{
    Task<IReadOnlyList<InsuranceDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<InsuranceDisplayModel> AddAsync(
        InsuranceFormModel model,
        CancellationToken cancellationToken = default);

    Task<InsuranceDisplayModel> UpdateAsync(
        InsuranceFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
