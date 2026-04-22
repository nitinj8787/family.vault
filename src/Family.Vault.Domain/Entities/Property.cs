using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a property or loan asset stored in the Family Vault for a given user.
/// </summary>
public sealed class Property
{
    public Property(
        Guid id,
        string userId,
        string assetName,
        PropertyCountry country,
        string ownership,
        bool loanLinked,
        string? documentsLocation)
    {
        Id = id;
        UserId = userId;
        AssetName = assetName;
        Country = country;
        Ownership = ownership;
        LoanLinked = loanLinked;
        DocumentsLocation = documentsLocation;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Human-readable name or address of the property.</summary>
    public string AssetName { get; }

    /// <summary>Country where the property is located (UK or India).</summary>
    public PropertyCountry Country { get; }

    /// <summary>Ownership description, e.g. "Sole", "Joint", "Company".</summary>
    public string Ownership { get; }

    /// <summary>Indicates whether a loan or mortgage is linked to this asset.</summary>
    public bool LoanLinked { get; }

    /// <summary>Location or reference where physical or digital documents are stored.</summary>
    public string? DocumentsLocation { get; }
}
