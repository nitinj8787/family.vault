using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Calculates the Family Readiness Score — a 0–100 composite metric that measures
/// how well a user has protected their family's financial future.
///
/// Factors (all weights are configurable via <c>ReadinessScoreOptions</c>):
/// <list type="bullet">
///   <item>Percentage of nominee-able assets that have a nominated beneficiary</item>
///   <item>Emergency fund coverage relative to the recommended minimum</item>
///   <item>Proportion of recorded jurisdictions that have a valid will</item>
///   <item>Whether any documents have been uploaded to the vault</item>
/// </list>
///
/// This service contains no UI logic and is safe to consume from any host (API, background
/// workers, scheduled jobs, etc.).
/// </summary>
public interface IReadinessScoreService
{
    /// <summary>
    /// Calculates the readiness score for the specified user.
    /// </summary>
    /// <param name="userId">Unique identifier of the authenticated user.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// A <see cref="ReadinessScoreResponse"/> containing the aggregate score and a
    /// per-category breakdown.
    /// </returns>
    Task<ReadinessScoreResponse> GetScoreAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
