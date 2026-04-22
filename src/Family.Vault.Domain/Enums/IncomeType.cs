namespace Family.Vault.Domain.Enums;

/// <summary>
/// Describes the type of income for a tax-summary entry.
/// </summary>
public enum IncomeType
{
    /// <summary>Employment salary or wages.</summary>
    Salary,

    /// <summary>Income from renting out property.</summary>
    Rental,

    /// <summary>Dividend payments from shares or funds.</summary>
    Dividend,

    /// <summary>Pension income (state or private).</summary>
    Pension,

    /// <summary>Freelance, consulting, or self-employment income.</summary>
    FreelanceOrSelfEmployed,

    /// <summary>Gains from the disposal of assets.</summary>
    CapitalGains,

    /// <summary>Any income type not covered by the above categories.</summary>
    Other
}
