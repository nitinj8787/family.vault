namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single emergency fund entry stored in the Family Vault for a given user.
/// An emergency fund entry describes cash or liquid assets held at a specific location
/// (e.g. a bank or savings account) together with instructions for accessing the funds.
/// </summary>
public sealed class EmergencyFund
{
    public EmergencyFund(
        Guid id,
        string userId,
        string location,
        decimal amount,
        string? accessInstructions)
    {
        Id = id;
        UserId = userId;
        Location = location;
        Amount = amount;
        AccessInstructions = accessInstructions;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Name of the bank, account, or location where the fund is held.</summary>
    public string Location { get; }

    /// <summary>Amount held (in the user's preferred currency).</summary>
    public decimal Amount { get; }

    /// <summary>Free-text instructions describing how to access the funds in an emergency.</summary>
    public string? AccessInstructions { get; }
}
