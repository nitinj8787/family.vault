using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for an India financial asset.
/// Derived advisory flags (<see cref="IsTaxableInIndia"/>, <see cref="IsRepatriable"/>)
/// are computed from <see cref="AccountType"/> and returned to callers so that
/// the UI can render appropriate badges and warnings without re-implementing
/// business logic.
/// </summary>
/// <param name="Id">Unique asset identifier.</param>
/// <param name="Category">Asset category.</param>
/// <param name="BankOrPlatform">Name of the bank, fund house, broker, or platform.</param>
/// <param name="AccountType">NRE or NRO classification.</param>
/// <param name="Repatriation">Fully or limitedly repatriable.</param>
/// <param name="Nominee">Nominated beneficiary, if any.</param>
/// <param name="IsTaxableInIndia">
/// <c>true</c> when <see cref="AccountType"/> is <see cref="NriAccountType.NRO"/>;
/// interest and income on NRO accounts is subject to Indian TDS.
/// </param>
/// <param name="IsRepatriable">
/// <c>true</c> when <see cref="AccountType"/> is <see cref="NriAccountType.NRE"/>;
/// NRE funds (principal + interest) are freely repatriable abroad.
/// </param>
public sealed record IndiaAssetResponse(
    Guid Id,
    IndiaAssetCategory Category,
    string BankOrPlatform,
    NriAccountType AccountType,
    RepatriationStatus Repatriation,
    string? Nominee,
    bool IsTaxableInIndia,
    bool IsRepatriable);
