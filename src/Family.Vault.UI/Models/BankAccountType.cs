namespace Family.Vault.UI.Models;

/// <summary>
/// Bank account type — mirrors <c>Family.Vault.Domain.Enums.BankAccountType</c>
/// so the UI project remains independent of the Domain assembly.
/// </summary>
public enum BankAccountType
{
    Savings,
    Current,
    FixedDeposit,
    RecurringDeposit,
    Salary,
    Other
}
