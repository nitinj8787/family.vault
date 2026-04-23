namespace Family.Vault.Application.Models;

/// <summary>
/// Score contribution from a single category within the Family Readiness Score.
/// </summary>
/// <param name="CategoryName">Human-readable name, e.g. "Nominees".</param>
/// <param name="Score">Points earned in this category (0 – <see cref="MaxScore"/>).</param>
/// <param name="MaxScore">Maximum points available for this category.</param>
/// <param name="Description">Short explanation of how the score was derived.</param>
public sealed record ScoreCategoryResult(
    string CategoryName,
    int    Score,
    int    MaxScore,
    string Description);
