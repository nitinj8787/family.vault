namespace Family.Vault.Application.Models;

/// <summary>
/// The Family Readiness Score for a user, together with a per-category breakdown.
/// </summary>
/// <param name="TotalScore">Aggregated readiness score in the range 0–100.</param>
/// <param name="MaxScore">
/// Maximum achievable score (always 100 after weight normalisation).
/// </param>
/// <param name="Categories">
/// Per-category breakdown so callers can render a detailed progress table
/// without re-implementing any scoring logic.
/// </param>
public sealed record ReadinessScoreResponse(
    int TotalScore,
    int MaxScore,
    IReadOnlyList<ScoreCategoryResult> Categories);
