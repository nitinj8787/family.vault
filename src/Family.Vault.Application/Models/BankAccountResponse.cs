using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a bank account.
/// The <see cref="MaskedAccountNumber"/> field exposes only the last four digits
/// of the account number so that sensitive data is not leaked to callers.
/// </summary>
/// <param name="Id">Unique account identifier.</param>
/// <param name="BankName">Name of the bank.</param>
/// <param name="AccountType">Type of account (Savings, Current, Fixed Deposit, etc.).</param>
/// <param name="MaskedAccountNumber">Account number with all but the last four characters replaced by '•'.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
public sealed record BankAccountResponse(
    Guid Id,
    string BankName,
    BankAccountType AccountType,
    string MaskedAccountNumber,
    string? Nominee);
