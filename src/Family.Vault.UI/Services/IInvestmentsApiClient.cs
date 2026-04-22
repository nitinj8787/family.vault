using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing investments via the FamilyVault API.
/// </summary>
public interface IInvestmentsApiClient
{
    /// <summary>Returns all investments for the current user.</summary>
    Task<IReadOnlyList<InvestmentDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new investment and returns the persisted display model.</summary>
    Task<InvestmentDisplayModel> AddAsync(
        InvestmentFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing investment and returns the updated display model.</summary>
    Task<InvestmentDisplayModel> UpdateAsync(
        InvestmentFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the investment with the given <paramref name="id"/>.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
