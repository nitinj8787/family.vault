using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single investment holding stored in the Family Vault for a given user.
/// </summary>
public sealed class Investment
{
    public Investment(
        Guid id,
        string userId,
        string platform,
        InvestmentType type,
        string accountId,
        string? nominee)
    {
        Id = id;
        UserId = userId;
        Platform = platform;
        Type = type;
        AccountId = accountId;
        Nominee = nominee;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Name of the investment platform or broker (e.g. "Vanguard", "Zerodha").</summary>
    public string Platform { get; }

    /// <summary>Category of investment (Stocks, Mutual Fund, Pension, Crypto).</summary>
    public InvestmentType Type { get; }

    /// <summary>
    /// Account or folio reference number on the platform.
    /// Callers are responsible for masking this value before returning it to untrusted clients.
    /// </summary>
    public string AccountId { get; }

    public string? Nominee { get; }
}
