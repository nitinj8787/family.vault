using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing India financial assets via the FamilyVault API.
/// </summary>
public interface IIndiaAssetsApiClient
{
    /// <summary>Returns all India assets for the current user.</summary>
    Task<IReadOnlyList<IndiaAssetDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new India asset and returns the persisted display model.</summary>
    Task<IndiaAssetDisplayModel> AddAsync(
        IndiaAssetFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing India asset and returns the updated display model.</summary>
    Task<IndiaAssetDisplayModel> UpdateAsync(
        IndiaAssetFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the asset with the given <paramref name="id"/>.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
