using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing emergency fund entries via the FamilyVault API.
/// </summary>
public interface IEmergencyFundApiClient
{
    Task<IReadOnlyList<EmergencyFundDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<EmergencyFundDisplayModel> AddAsync(
        EmergencyFundFormModel model,
        CancellationToken cancellationToken = default);

    Task<EmergencyFundDisplayModel> UpdateAsync(
        EmergencyFundFormModel model,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
