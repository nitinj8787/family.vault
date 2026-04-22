namespace Family.Vault.Domain.Enums;

/// <summary>
/// Categorises an India financial asset in the Family Vault.
/// </summary>
public enum IndiaAssetCategory
{
    /// <summary>Bank savings or current account.</summary>
    BankAccount,

    /// <summary>Mutual fund investment.</summary>
    MutualFund,

    /// <summary>Fixed deposit.</summary>
    FixedDeposit,

    /// <summary>Listed or unlisted stocks / equities.</summary>
    Stocks,

    /// <summary>Real-estate or property investment.</summary>
    Property,

    /// <summary>Life insurance or ULIP policy.</summary>
    Insurance,

    /// <summary>Any other India-based financial asset.</summary>
    Other
}
