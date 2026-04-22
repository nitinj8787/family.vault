using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing UK financial assets via the FamilyVault API.
/// </summary>
public interface IUkAssetsApiClient
{
    /// <summary>Returns all UK assets for the current user.</summary>
    Task<IReadOnlyList<UkAssetDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new UK asset and returns the persisted display model (with masked account number).
    /// </summary>
    Task<UkAssetDisplayModel> AddAsync(
        UkAssetFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing UK asset and returns the updated display model.
    /// </summary>
    Task<UkAssetDisplayModel> UpdateAsync(
        UkAssetFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the asset with the given <paramref name="id"/>.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
