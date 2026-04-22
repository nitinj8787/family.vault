using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single UK financial asset (bank account, ISA, pension, etc.)
/// stored in the Family Vault for a given user.
/// </summary>
public sealed class UkAsset
{
    public UkAsset(
        Guid id,
        string userId,
        UkAssetCategory category,
        string provider,
        string accountNumber,
        string? nominee,
        string? taxNotes)
    {
        Id = id;
        UserId = userId;
        Category = category;
        Provider = provider;
        AccountNumber = accountNumber;
        Nominee = nominee;
        TaxNotes = taxNotes;
    }

    public Guid Id { get; }
    public string UserId { get; }
    public UkAssetCategory Category { get; }
    public string Provider { get; }

    /// <summary>
    /// Full account number. Callers are responsible for masking this value
    /// before returning it to untrusted clients.
    /// </summary>
    public string AccountNumber { get; }

    public string? Nominee { get; }
    public string? TaxNotes { get; }
}
