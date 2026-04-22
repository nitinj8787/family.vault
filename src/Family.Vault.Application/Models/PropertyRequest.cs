using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating a property / loan asset.
/// </summary>
/// <param name="AssetName">Human-readable name or address of the property.</param>
/// <param name="Country">Country where the property is located (UK or India).</param>
/// <param name="Ownership">Ownership description, e.g. "Sole", "Joint", "Company".</param>
/// <param name="LoanLinked">Whether a loan or mortgage is associated with this asset.</param>
/// <param name="DocumentsLocation">Location or reference for physical/digital documents.</param>
public sealed record PropertyRequest(
    string AssetName,
    PropertyCountry Country,
    string Ownership,
    bool LoanLinked,
    string? DocumentsLocation);
