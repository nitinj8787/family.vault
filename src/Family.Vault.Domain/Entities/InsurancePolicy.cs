namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single insurance policy stored in the Family Vault for a given user.
/// </summary>
public sealed class InsurancePolicy
{
    public InsurancePolicy(
        Guid id,
        string userId,
        string provider,
        string policyType,
        string policyNumber,
        string coverage,
        string? nominee,
        string claimContact,
        DateOnly? expiryDate = null)
    {
        Id = id;
        UserId = userId;
        Provider = provider;
        PolicyType = policyType;
        PolicyNumber = policyNumber;
        Coverage = coverage;
        Nominee = nominee;
        ClaimContact = claimContact;
        ExpiryDate = expiryDate;
    }

    public Guid Id { get; }
    public string UserId { get; }
    public string Provider { get; }
    public string PolicyType { get; }
    public string PolicyNumber { get; }
    public string Coverage { get; }
    public string? Nominee { get; }
    public string ClaimContact { get; }

    /// <summary>
    /// Optional expiry date of the policy. Used to surface "expiring soon" insights
    /// in the Dashboard.
    /// </summary>
    public DateOnly? ExpiryDate { get; }
}

