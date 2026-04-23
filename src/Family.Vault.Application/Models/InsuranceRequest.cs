namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating an insurance policy.
/// </summary>
/// <param name="Provider">Name of the insurance provider.</param>
/// <param name="PolicyType">Type of policy (e.g. health, life, vehicle).</param>
/// <param name="PolicyNumber">Policy identifier issued by the provider.</param>
/// <param name="Coverage">Coverage details or insured amount.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
/// <param name="ClaimContact">Claim support contact details.</param>
/// <param name="ExpiryDate">Optional policy expiry date. Used to surface expiring-soon insights.</param>
public sealed record InsuranceRequest(
    string Provider,
    string PolicyType,
    string PolicyNumber,
    string Coverage,
    string? Nominee,
    string ClaimContact,
    DateOnly? ExpiryDate = null);

