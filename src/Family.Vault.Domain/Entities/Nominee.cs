using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single nominee assignment for a family-vault asset.
/// A nominee is the person designated to inherit or receive the asset in the event of the owner's death.
/// </summary>
public sealed class Nominee
{
    public Nominee(
        Guid id,
        string userId,
        NomineeAssetType assetType,
        string institution,
        string nomineeName,
        string relationship)
    {
        Id = id;
        UserId = userId;
        AssetType = assetType;
        Institution = institution;
        NomineeName = nomineeName;
        Relationship = relationship;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Category of the asset for which this nominee is assigned.</summary>
    public NomineeAssetType AssetType { get; }

    /// <summary>Name of the bank, insurer, broker, or other institution holding the asset.</summary>
    public string Institution { get; }

    /// <summary>Full legal name of the nominee person.</summary>
    public string NomineeName { get; }

    /// <summary>Relationship of the nominee to the account owner (e.g. "Spouse", "Child").</summary>
    public string Relationship { get; }
}
