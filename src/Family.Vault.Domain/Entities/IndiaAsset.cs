using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single India-based financial asset (bank account, mutual fund,
/// fixed deposit, etc.) stored in the Family Vault for a given NRI user.
/// </summary>
public sealed class IndiaAsset
{
    public IndiaAsset(
        Guid id,
        string userId,
        IndiaAssetCategory category,
        string bankOrPlatform,
        NriAccountType accountType,
        RepatriationStatus repatriation,
        string? nominee)
    {
        Id = id;
        UserId = userId;
        Category = category;
        BankOrPlatform = bankOrPlatform;
        AccountType = accountType;
        Repatriation = repatriation;
        Nominee = nominee;
    }

    public Guid Id { get; }
    public string UserId { get; }
    public IndiaAssetCategory Category { get; }

    /// <summary>Name of the bank, fund house, broker, or platform.</summary>
    public string BankOrPlatform { get; }

    /// <summary>NRE or NRO account classification.</summary>
    public NriAccountType AccountType { get; }

    /// <summary>
    /// Whether the asset is fully or only limitedly repatriable.
    /// Validated against <see cref="AccountType"/>: an NRE account must be
    /// <see cref="RepatriationStatus.FullyRepatriable"/>; an NRO account must be
    /// <see cref="RepatriationStatus.Limited"/>.
    /// </summary>
    public RepatriationStatus Repatriation { get; }

    public string? Nominee { get; }
}
