namespace Family.Vault.UI.Services;

/// <summary>
/// Development / testing token provider that returns a static bearer token read
/// from the <c>VaultApi:BearerToken</c> configuration key.
/// When the key is absent or empty the provider returns an empty string and the
/// API request is sent without an Authorization header value — useful for local
/// runs against an API with authentication disabled.
/// </summary>
/// <remarks>
/// <b>Do not use this provider in production.</b>
/// Switch to <see cref="AzureAdTokenProvider"/> by setting
/// <c>VaultApi:UseAzureAdAuth</c> to <c>true</c> in your production appsettings.
/// </remarks>
public sealed class PlaceholderTokenProvider(IConfiguration configuration) : ITokenProvider
{
    private readonly string _token =
        configuration["VaultApi:BearerToken"] ?? string.Empty;

    /// <inheritdoc/>
    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_token);
}
