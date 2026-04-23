namespace Family.Vault.Application.Models;

/// <summary>
/// Indicates how urgent an actionable insight is.
/// </summary>
public enum InsightSeverity
{
    /// <summary>Informational — worth addressing but not immediately urgent.</summary>
    Low,

    /// <summary>Moderate risk — should be addressed in the near term.</summary>
    Medium,

    /// <summary>High risk — requires immediate attention.</summary>
    High
}
