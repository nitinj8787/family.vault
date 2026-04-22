using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating a UK financial asset.
/// </summary>
/// <param name="Category">Asset category (Bank, ISA, Pension).</param>
/// <param name="Provider">Name of the financial institution or provider.</param>
/// <param name="AccountNumber">Full account / reference number.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
/// <param name="TaxNotes">Free-text tax or inheritance notes, if any.</param>
public sealed record UkAssetRequest(
    UkAssetCategory Category,
    string Provider,
    string AccountNumber,
    string? Nominee,
    string? TaxNotes);
