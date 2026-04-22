namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a will / legal record for a specific country stored in the Family Vault.
/// Each entry tracks whether a valid will exists for that jurisdiction, where the will
/// is located, and who the appointed executor is.
/// </summary>
public sealed class WillEntry
{
    public WillEntry(
        Guid id,
        string userId,
        string country,
        bool willExists,
        string? location,
        string? executor)
    {
        Id = id;
        UserId = userId;
        Country = country;
        WillExists = willExists;
        Location = location;
        Executor = executor;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Jurisdiction / country the will covers (e.g. "United Kingdom", "India").</summary>
    public string Country { get; }

    /// <summary>Whether a signed, valid will exists for this jurisdiction.</summary>
    public bool WillExists { get; }

    /// <summary>
    /// Physical or digital location of the will (e.g. "Solicitor – Smith &amp; Co",
    /// "Safe deposit box – HSBC Manchester", "Cloud – 1Password").
    /// Null/empty when <see cref="WillExists"/> is <c>false</c>.
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// Full name of the appointed executor.
    /// Null/empty when <see cref="WillExists"/> is <c>false</c>.
    /// </summary>
    public string? Executor { get; }
}
