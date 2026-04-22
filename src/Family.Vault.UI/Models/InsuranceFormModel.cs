using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Insurance Manager add/edit dialog.</summary>
public sealed class InsuranceFormModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [MaxLength(150, ErrorMessage = "Provider must not exceed 150 characters.")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "Policy type is required.")]
    [MaxLength(100, ErrorMessage = "Policy type must not exceed 100 characters.")]
    public string PolicyType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Policy number is required.")]
    [MaxLength(60, ErrorMessage = "Policy number must not exceed 60 characters.")]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Coverage is required.")]
    [MaxLength(120, ErrorMessage = "Coverage must not exceed 120 characters.")]
    public string Coverage { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Nominee name must not exceed 100 characters.")]
    public string? Nominee { get; set; }

    [Required(ErrorMessage = "Claim contact is required.")]
    [MaxLength(120, ErrorMessage = "Claim contact must not exceed 120 characters.")]
    public string ClaimContact { get; set; } = string.Empty;

    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class InsuranceDisplayModel
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string Coverage { get; set; } = string.Empty;
    public string? Nominee { get; set; }
    public string ClaimContact { get; set; } = string.Empty;
}
