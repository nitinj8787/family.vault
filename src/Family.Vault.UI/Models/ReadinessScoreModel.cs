namespace Family.Vault.UI.Models;

/// <summary>Score contribution from a single category, populated from the API response.</summary>
public sealed class ScoreCategoryModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int Score    { get; set; }
    public int MaxScore { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Percentage of MaxScore earned (0–100), used for CSS width styling.</summary>
    public int PercentFill => MaxScore > 0 ? (int)Math.Round(100.0 * Score / MaxScore) : 0;

    /// <summary>True when the category is fully scored.</summary>
    public bool IsFullScore => Score >= MaxScore && MaxScore > 0;
}

/// <summary>Family Readiness Score response, populated from <c>GET /api/dashboard/score</c>.</summary>
public sealed class ReadinessScoreModel
{
    public int TotalScore { get; set; }
    public int MaxScore   { get; set; }
    public List<ScoreCategoryModel> Categories { get; set; } = [];

    /// <summary>Colour key for the score progress bar.</summary>
    public string FillColor => TotalScore >= 80 ? "good" : TotalScore >= 50 ? "warn" : "low";

    /// <summary>Human-readable description of the overall score level.</summary>
    public string Description => TotalScore >= 80
        ? "Great – your vault is well protected."
        : TotalScore >= 50
            ? "Moderate – a few gaps to address."
            : "Low – important items are missing.";
}
