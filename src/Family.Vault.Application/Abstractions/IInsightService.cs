using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Analyses a user's vault data and produces a prioritised list of actionable insights
/// for display on the Dashboard.
///
/// Detects:
/// <list type="bullet">
///   <item>Missing nominees on assets</item>
///   <item>No signed will on record</item>
///   <item>Low or missing emergency fund</item>
///   <item>Insurance policies expiring within 30 days</item>
///   <item>Assets recorded but no documents uploaded</item>
/// </list>
///
/// This service contains no UI logic and is safe to consume from any host (API, background
/// workers, etc.).
/// </summary>
public interface IInsightService
{
    /// <summary>
    /// Generates insights for the specified user.
    /// </summary>
    /// <param name="userId">Unique identifier of the authenticated user.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// An ordered list of insights sorted by descending <see cref="InsightResponse.Severity"/>.
    /// Returns an empty list when no gaps are detected.
    /// </returns>
    Task<IReadOnlyList<InsightResponse>> GetInsightsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
