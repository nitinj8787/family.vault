using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing bank accounts via the FamilyVault API.
/// </summary>
public interface IBankAccountsApiClient
{
    /// <summary>Returns all bank accounts for the current user.</summary>
    Task<IReadOnlyList<BankAccountDisplayModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new bank account and returns the persisted display model.</summary>
    Task<BankAccountDisplayModel> AddAsync(
        BankAccountFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing bank account and returns the updated display model.</summary>
    Task<BankAccountDisplayModel> UpdateAsync(
        BankAccountFormModel model,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the account with the given <paramref name="id"/>.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
