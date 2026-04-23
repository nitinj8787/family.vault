using Family.Vault.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Secrets;

/// <summary>
/// Development-only implementation of <see cref="IKeyVaultService"/> that keeps secrets in
/// an in-memory dictionary.  Pre-seeded values are read from the <c>LocalDev:Secrets</c>
/// configuration section on startup.  Replaces <see cref="KeyVaultService"/> when running
/// locally without an Azure subscription.
/// </summary>
/// <remarks>
/// <b>Do not use in production.</b>  Enable by setting <c>LocalDev:Enabled = true</c> in
/// <c>appsettings.Development.json</c>.
/// </remarks>
public sealed class LocalKeyVaultService : IKeyVaultService
{
    private readonly Dictionary<string, string> _secrets;
    private readonly ILogger<LocalKeyVaultService> _logger;

    public LocalKeyVaultService(IConfiguration configuration, ILogger<LocalKeyVaultService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Pre-seed from LocalDev:Secrets so tests can inject known values.
        var seedSection = configuration.GetSection("LocalDev:Secrets");
        foreach (var child in seedSection.GetChildren())
        {
            if (!string.IsNullOrEmpty(child.Value))
                _secrets[child.Key] = child.Value;
        }
    }

    /// <inheritdoc/>
    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("Secret name is required.", nameof(secretName));

        if (_secrets.TryGetValue(secretName, out var value))
        {
            _logger.LogInformation("LocalKeyVaultService: retrieved secret {SecretName}", secretName);
            return Task.FromResult(value);
        }

        _logger.LogWarning("LocalKeyVaultService: secret {SecretName} not found", secretName);
        throw new KeyNotFoundException($"Secret '{secretName}' was not found in the local key vault store.");
    }

    /// <inheritdoc/>
    public Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("Secret name is required.", nameof(secretName));

        ArgumentNullException.ThrowIfNull(secretValue);

        _secrets[secretName] = secretValue;
        _logger.LogInformation("LocalKeyVaultService: stored secret {SecretName}", secretName);
        return Task.CompletedTask;
    }
}
