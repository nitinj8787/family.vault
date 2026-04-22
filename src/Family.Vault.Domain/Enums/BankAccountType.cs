namespace Family.Vault.Domain.Enums;

/// <summary>
/// Classifies the type of a bank account held in the Family Vault.
/// </summary>
public enum BankAccountType
{
    /// <summary>Standard savings account.</summary>
    Savings,

    /// <summary>Current (cheque) account, typically used for business or high-volume transactions.</summary>
    Current,

    /// <summary>Fixed deposit — lump sum locked for a fixed term at a fixed interest rate.</summary>
    FixedDeposit,

    /// <summary>Recurring deposit — regular monthly contributions at a fixed interest rate.</summary>
    RecurringDeposit,

    /// <summary>Salary account linked to an employer payroll.</summary>
    Salary,

    /// <summary>Any other bank account type not covered above.</summary>
    Other
}
