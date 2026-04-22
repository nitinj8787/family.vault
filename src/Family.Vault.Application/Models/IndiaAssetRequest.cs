using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating an India financial asset.
/// </summary>
/// <param name="Category">Asset category (BankAccount, MutualFund, etc.).</param>
/// <param name="BankOrPlatform">Name of the bank, fund house, broker, or platform.</param>
/// <param name="AccountType">NRE or NRO account classification.</param>
/// <param name="Repatriation">Whether funds are fully or only limitedly repatriable.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
public sealed record IndiaAssetRequest(
    IndiaAssetCategory Category,
    string BankOrPlatform,
    NriAccountType AccountType,
    RepatriationStatus Repatriation,
    string? Nominee);
