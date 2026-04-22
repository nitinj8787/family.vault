using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a nominee entry returned by <c>INomineeService</c>.
/// </summary>
/// <param name="Id">Unique nominee identifier.</param>
/// <param name="AssetType">Category of the asset.</param>
/// <param name="Institution">Name of the institution holding the asset.</param>
/// <param name="NomineeName">Full legal name of the nominee.</param>
/// <param name="Relationship">Relationship of the nominee to the account owner.</param>
public sealed record NomineeResponse(
    Guid Id,
    NomineeAssetType AssetType,
    string Institution,
    string NomineeName,
    string Relationship);
