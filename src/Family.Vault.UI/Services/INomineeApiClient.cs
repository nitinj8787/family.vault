using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing nominee entries via the FamilyVault API.
/// </summary>
public interface INomineeApiClient
{
    Task<IReadOnlyList<NomineeDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<NomineeDisplayModel> AddAsync(
        NomineeFormModel model,
        CancellationToken cancellationToken = default);

    Task<NomineeDisplayModel> UpdateAsync(
        NomineeFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
