using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating an investment.
/// </summary>
/// <param name="Platform">Name of the investment platform or broker.</param>
/// <param name="Type">Category of investment (Stocks, MutualFund, Pension, Crypto).</param>
/// <param name="AccountId">Account or folio reference number on the platform.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
public sealed record InvestmentRequest(
    string Platform,
    InvestmentType Type,
    string AccountId,
    string? Nominee);
