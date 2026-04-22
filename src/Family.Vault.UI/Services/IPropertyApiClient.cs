using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing property/loan assets via the FamilyVault API.
/// </summary>
public interface IPropertyApiClient
{
    Task<IReadOnlyList<PropertyDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PropertyDisplayModel> AddAsync(
        PropertyFormModel model,
        CancellationToken cancellationToken = default);

    Task<PropertyDisplayModel> UpdateAsync(
        PropertyFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
