using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Secrets;

/// <summary>
/// Production-ready Azure Key Vault implementation of <see cref="IKeyVaultService"/>.
/// Authenticates using the provided <see cref="TokenCredential"/> (e.g. <c>DefaultAzureCredential</c>
/// for Managed Identity / Azure AD) — no connection strings or raw credentials are stored.
/// </summary>
/// <remarks>
/// Retry behaviour is delegated to the Azure SDK's built-in policy, which applies exponential
/// back-off with jitter for transient 429 / 5xx responses.  The policy is configured in the
/// constructor via <see cref="SecretClientOptions.Retry"/>.
/// </remarks>
public sealed class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _client;
    private readonly ILogger<KeyVaultService> _logger;

    /// <param name="vaultUri">
    ///   Full URI of the Key Vault, e.g. <c>https://&lt;vault-name&gt;.vault.azure.net</c>.
    /// </param>
    /// <param name="credential">
    ///   Azure AD token credential (e.g. <c>DefaultAzureCredential</c>).
    ///   Register as a singleton in DI to benefit from token caching.
    /// </param>
    /// <param name="logger">Structured logger injected by the DI container.</param>
    public KeyVaultService(Uri vaultUri, TokenCredential credential, ILogger<KeyVaultService> logger)
    {
        ArgumentNullException.ThrowIfNull(vaultUri);
        ArgumentNullException.ThrowIfNull(credential);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure the SDK's built-in retry policy: up to 3 retries using exponential back-off.
        // The Azure SDK will automatically retry transient failures (429, 500, 502, 503, 504)
        // without requiring a third-party library.
        var options = new SecretClientOptions
        {
            Retry =
            {
                MaxRetries = 3,
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30),
                NetworkTimeout = TimeSpan.FromSeconds(30)
            }
        };

        _client = new SecretClient(vaultUri, credential, options);
    }

    /// <inheritdoc/>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name is required.", nameof(secretName));
        }

        _logger.LogInformation("Retrieving secret {SecretName} from Key Vault", LogSanitizer.Sanitize(secretName));

        try
        {
            var response = await _client.GetSecretAsync(secretName, version: null, cancellationToken);
            _logger.LogInformation("Successfully retrieved secret {SecretName}", LogSanitizer.Sanitize(secretName));
            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} was not found in Key Vault", LogSanitizer.Sanitize(secretName));
            throw new KeyNotFoundException($"Secret '{secretName}' was not found in Key Vault.", ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Key Vault request failed while retrieving secret {SecretName}. Status: {Status}",
                LogSanitizer.Sanitize(secretName), ex.Status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name is required.", nameof(secretName));
        }

        ArgumentNullException.ThrowIfNull(secretValue);

        _logger.LogInformation("Setting secret {SecretName} in Key Vault", LogSanitizer.Sanitize(secretName));

        try
        {
            await _client.SetSecretAsync(secretName, secretValue, cancellationToken);
            _logger.LogInformation("Successfully set secret {SecretName}", LogSanitizer.Sanitize(secretName));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Key Vault request failed while setting secret {SecretName}. Status: {Status}",
                LogSanitizer.Sanitize(secretName), ex.Status);
            throw;
        }
    }
}
