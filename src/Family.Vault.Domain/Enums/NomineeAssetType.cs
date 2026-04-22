namespace Family.Vault.Domain.Enums;

/// <summary>
/// Describes the category of asset to which a nominee is assigned.
/// </summary>
public enum NomineeAssetType
{
    /// <summary>Bank savings or current account.</summary>
    BankAccount,

    /// <summary>Stocks, mutual funds, shares, or other investments.</summary>
    Investment,

    /// <summary>Life, health, or general insurance policy.</summary>
    Insurance,

    /// <summary>Residential, commercial, or land property.</summary>
    Property,

    /// <summary>Pension pot or retirement fund.</summary>
    Pension,

    /// <summary>Any asset not covered by the above categories.</summary>
    Other
}
