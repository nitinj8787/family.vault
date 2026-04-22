namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for an insurance policy.
/// </summary>
/// <param name="Id">Unique policy identifier.</param>
/// <param name="Provider">Name of the insurance provider.</param>
/// <param name="PolicyType">Type of policy.</param>
/// <param name="PolicyNumber">Policy identifier issued by the provider.</param>
/// <param name="Coverage">Coverage details or insured amount.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
/// <param name="ClaimContact">Claim support contact details.</param>
public sealed record InsuranceResponse(
    Guid Id,
    string Provider,
    string PolicyType,
    string PolicyNumber,
    string Coverage,
    string? Nominee,
    string ClaimContact);
