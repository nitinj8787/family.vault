using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing tax-summary entries via the FamilyVault API.
/// </summary>
public interface ITaxApiClient
{
    Task<IReadOnlyList<TaxDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaxDisplayModel> AddAsync(
        TaxFormModel model,
        CancellationToken cancellationToken = default);

    Task<TaxDisplayModel> UpdateAsync(
        TaxFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
