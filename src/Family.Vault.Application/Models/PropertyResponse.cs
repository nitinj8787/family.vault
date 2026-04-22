using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a property / loan asset.
/// </summary>
/// <param name="Id">Unique asset identifier.</param>
/// <param name="AssetName">Human-readable name or address of the property.</param>
/// <param name="Country">Country where the property is located (UK or India).</param>
/// <param name="Ownership">Ownership description.</param>
/// <param name="LoanLinked">Whether a loan or mortgage is associated with this asset.</param>
/// <param name="DocumentsLocation">Location or reference for physical/digital documents.</param>
public sealed record PropertyResponse(
    Guid Id,
    string AssetName,
    PropertyCountry Country,
    string Ownership,
    bool LoanLinked,
    string? DocumentsLocation);
