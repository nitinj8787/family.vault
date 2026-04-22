using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the UK Assets add/edit dialog.</summary>
public sealed class UkAssetFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public UkAssetCategory Category { get; set; } = UkAssetCategory.Bank;

    [Required(ErrorMessage = "Provider is required.")]
    [MaxLength(150, ErrorMessage = "Provider must not exceed 150 characters.")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account number is required.")]
    [MaxLength(50, ErrorMessage = "Account number must not exceed 50 characters.")]
    public string AccountNumber { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Nominee name must not exceed 100 characters.")]
    public string? Nominee { get; set; }

    [MaxLength(500, ErrorMessage = "Tax notes must not exceed 500 characters.")]
    public string? TaxNotes { get; set; }

    /// <summary>Whether the form represents an existing record (update) vs a new one (add).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class UkAssetDisplayModel
{
    public Guid Id { get; set; }
    public UkAssetCategory Category { get; set; }
    public string Provider { get; set; } = string.Empty;

    /// <summary>Masked account number returned by the API (e.g. "•••••••1234").</summary>
    public string MaskedAccountNumber { get; set; } = string.Empty;

    public string? Nominee { get; set; }
    public string? TaxNotes { get; set; }
}
