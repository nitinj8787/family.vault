namespace Family.Vault.UI.Models;

/// <summary>
/// Investment type — mirrors <c>Family.Vault.Domain.Enums.InvestmentType</c>
/// so the UI project remains independent of the Domain assembly.
/// </summary>
public enum InvestmentType
{
    Stocks,
    MutualFund,
    Pension,
    Crypto
}
