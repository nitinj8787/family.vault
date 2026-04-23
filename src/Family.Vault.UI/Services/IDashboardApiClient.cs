using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API dashboard endpoints.
/// </summary>
public interface IDashboardApiClient
{
    /// <summary>
    /// Fetches the Family Readiness Score for the currently authenticated user.
    /// </summary>
    Task<ReadinessScoreModel> GetReadinessScoreAsync(CancellationToken cancellationToken = default);
}
