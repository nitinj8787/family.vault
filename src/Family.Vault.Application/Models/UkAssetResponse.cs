using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a UK financial asset.
/// The <see cref="MaskedAccountNumber"/> field exposes only the last four digits
/// of the account number so that sensitive data is not leaked to callers.
/// </summary>
/// <param name="Id">Unique asset identifier.</param>
/// <param name="Category">Asset category (Bank, ISA, Pension).</param>
/// <param name="Provider">Name of the financial institution or provider.</param>
/// <param name="MaskedAccountNumber">Account number with all but the last four characters replaced by '•'.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
/// <param name="TaxNotes">Free-text tax or inheritance notes, if any.</param>
public sealed record UkAssetResponse(
    Guid Id,
    UkAssetCategory Category,
    string Provider,
    string MaskedAccountNumber,
    string? Nominee,
    string? TaxNotes);
