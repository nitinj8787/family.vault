namespace Family.Vault.UI.Services;

/// <summary>
/// Provides a bearer token to be included in outgoing API requests.
/// Implementations control how the token is acquired (e.g. Azure AD token cache,
/// static placeholder for development, or an external identity provider).
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Returns a bearer token string.  
    /// Implementations should return an empty string (not <c>null</c>) when no
    /// token is available, so callers can unconditionally set the Authorization header.
    /// </summary>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
