using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Manages the personal profile for a Family Vault user.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Returns the profile for <paramref name="userId"/>, or <c>null</c> if no profile has
    /// been saved yet.
    /// </summary>
    Task<ProfileResponse?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces the profile for <paramref name="userId"/>.
    /// </summary>
    /// <exception cref="Exceptions.ProfileValidationException">
    /// Thrown when the request fails validation (e.g. missing required fields).
    /// </exception>
    Task<ProfileResponse> SaveProfileAsync(
        string userId,
        ProfileRequest request,
        CancellationToken cancellationToken = default);
}
