using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing wills &amp; legal entries via the FamilyVault API.
/// </summary>
public interface IWillsApiClient
{
    Task<IReadOnlyList<WillDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WillDisplayModel> AddAsync(
        WillFormModel model,
        CancellationToken cancellationToken = default);

    Task<WillDisplayModel> UpdateAsync(
        WillFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
