namespace Family.Vault.Domain.Enums;

/// <summary>
/// Classifies the type of an investment held in the Family Vault.
/// </summary>
public enum InvestmentType
{
    /// <summary>Direct equity holdings (shares / ETFs).</summary>
    Stocks,

    /// <summary>Mutual fund or index fund units.</summary>
    MutualFund,

    /// <summary>Workplace or personal pension scheme.</summary>
    Pension,

    /// <summary>Cryptocurrency holdings (Bitcoin, Ethereum, etc.).</summary>
    Crypto
}
