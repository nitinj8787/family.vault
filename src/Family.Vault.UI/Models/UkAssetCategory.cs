namespace Family.Vault.UI.Models;

/// <summary>
/// UK financial asset category — mirrors <c>Family.Vault.Domain.Enums.UkAssetCategory</c>
/// so the UI project remains independent of the Domain assembly.
/// </summary>
public enum UkAssetCategory
{
    Bank,
    ISA,
    Pension
}
