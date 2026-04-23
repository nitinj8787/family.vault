namespace Family.Vault.Application.Configuration;

/// <summary>
/// Configuration options that control how the Family Readiness Score is calculated.
/// Bind to the <c>ReadinessScore</c> section of application settings.
///
/// Each <c>Weight</c> property represents the maximum number of points that category
/// contributes to the 0–100 score.  Weights are normalised at runtime so the actual
/// ceiling is always 100, regardless of whether the values sum to exactly 100.
/// </summary>
public sealed class ReadinessScoreOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ReadinessScore";

    /// <summary>
    /// Maximum points awarded for nominee coverage across all nominee-able assets.
    /// Default: 25.
    /// </summary>
    public int NomineesWeight { get; init; } = 25;

    /// <summary>
    /// Maximum points awarded for having an adequate emergency fund.
    /// Default: 25.
    /// </summary>
    public int EmergencyFundWeight { get; init; } = 25;

    /// <summary>
    /// Maximum points awarded for having a valid will on record.
    /// Default: 25.
    /// </summary>
    public int WillsWeight { get; init; } = 25;

    /// <summary>
    /// Maximum points awarded for uploading documents to the vault.
    /// Default: 25.
    /// </summary>
    public int DocumentsWeight { get; init; } = 25;

    /// <summary>
    /// The emergency-fund total (in the user's stored currency) at or above which
    /// the full emergency-fund score is awarded.
    /// Default: 1000.
    /// </summary>
    public decimal MinimumRecommendedEmergencyFund { get; init; } = 1_000m;
}
