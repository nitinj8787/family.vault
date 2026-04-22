using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating a bank account.
/// </summary>
/// <param name="BankName">Name of the bank (e.g. "HDFC Bank", "State Bank of India").</param>
/// <param name="AccountType">Type of account (Savings, Current, Fixed Deposit, etc.).</param>
/// <param name="AccountNumber">Full account number.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
public sealed record BankAccountRequest(
    string BankName,
    BankAccountType AccountType,
    string AccountNumber,
    string? Nominee);
