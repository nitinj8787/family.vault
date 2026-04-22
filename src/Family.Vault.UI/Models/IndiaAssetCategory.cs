namespace Family.Vault.UI.Models;

/// <summary>
/// India financial asset category — mirrors <c>Family.Vault.Domain.Enums.IndiaAssetCategory</c>
/// so the UI project remains independent of the Domain assembly.
/// </summary>
public enum IndiaAssetCategory
{
    BankAccount,
    MutualFund,
    FixedDeposit,
    Stocks,
    Property,
    Insurance,
    Other
}
