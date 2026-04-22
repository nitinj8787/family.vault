using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating a nominee entry.
/// </summary>
/// <param name="AssetType">Category of asset for which the nominee is assigned.</param>
/// <param name="Institution">Name of the institution holding the asset.</param>
/// <param name="NomineeName">Full legal name of the nominee.</param>
/// <param name="Relationship">Relationship of the nominee to the account owner.</param>
public sealed record NomineeRequest(
    NomineeAssetType AssetType,
    string Institution,
    string NomineeName,
    string Relationship);
