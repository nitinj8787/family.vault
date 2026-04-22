namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Abstraction over Azure Key Vault secrets operations.
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Retrieves the current value of a secret from Key Vault.
    /// </summary>
    /// <param name="secretName">Name of the secret to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value string.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the secret does not exist or is disabled.</exception>
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a secret in Key Vault.
    /// </summary>
    /// <param name="secretName">Name of the secret to set.</param>
    /// <param name="secretValue">Value to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);
}
