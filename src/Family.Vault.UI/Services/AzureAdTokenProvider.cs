using Microsoft.Identity.Web;

namespace Family.Vault.UI.Services;

/// <summary>
/// Production token provider that acquires a bearer token for the currently
/// authenticated user via <see cref="ITokenAcquisition"/> (MSAL / Azure AD).
/// Tokens are automatically cached and refreshed by the underlying MSAL
/// in-memory token cache registered in <c>Program.cs</c>.
/// </summary>
public sealed class AzureAdTokenProvider(
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration) : ITokenProvider
{
    private readonly string[] _apiScopes =
        configuration.GetSection("VaultApi:Scopes").Get<string[]>()
        ?? throw new InvalidOperationException("VaultApi:Scopes is not configured.");

    /// <inheritdoc/>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default) =>
        await tokenAcquisition.GetAccessTokenForUserAsync(_apiScopes);
}
