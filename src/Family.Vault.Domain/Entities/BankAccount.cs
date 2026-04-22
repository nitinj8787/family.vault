using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single bank account stored in the Family Vault for a given user.
/// </summary>
public sealed class BankAccount
{
    public BankAccount(
        Guid id,
        string userId,
        string bankName,
        BankAccountType accountType,
        string accountNumber,
        string? nominee)
    {
        Id = id;
        UserId = userId;
        BankName = bankName;
        AccountType = accountType;
        AccountNumber = accountNumber;
        Nominee = nominee;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Name of the bank (e.g. "HDFC Bank", "State Bank of India").</summary>
    public string BankName { get; }

    /// <summary>Type of the account (Savings, Current, Fixed Deposit, etc.).</summary>
    public BankAccountType AccountType { get; }

    /// <summary>
    /// Full account number. Callers are responsible for masking this value
    /// before returning it to untrusted clients.
    /// </summary>
    public string AccountNumber { get; }

    public string? Nominee { get; }
}
