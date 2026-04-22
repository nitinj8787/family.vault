using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for reading and writing the user's personal profile via the FamilyVault API.
/// </summary>
public interface IProfileApiClient
{
    /// <summary>
    /// Returns the current user's profile, or <c>null</c> if no profile has been saved yet.
    /// </summary>
    Task<ProfileFormModel?> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves (creates or replaces) the current user's profile and returns the persisted state.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the API returns a non-success status code.
    /// </exception>
    Task<ProfileFormModel> SaveProfileAsync(
        ProfileFormModel model,
        CancellationToken cancellationToken = default);
}
