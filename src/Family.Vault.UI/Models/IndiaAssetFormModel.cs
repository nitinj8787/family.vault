using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the India Assets add/edit dialog.</summary>
public sealed class IndiaAssetFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public IndiaAssetCategory Category { get; set; } = IndiaAssetCategory.BankAccount;

    [Required(ErrorMessage = "Bank / platform is required.")]
    [MaxLength(150, ErrorMessage = "Bank / platform must not exceed 150 characters.")]
    public string BankOrPlatform { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required.")]
    public NriAccountType AccountType { get; set; } = NriAccountType.NRE;

    [Required(ErrorMessage = "Repatriation status is required.")]
    public RepatriationStatus Repatriation { get; set; } = RepatriationStatus.FullyRepatriable;

    [MaxLength(100, ErrorMessage = "Nominee name must not exceed 100 characters.")]
    public string? Nominee { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class IndiaAssetDisplayModel
{
    public Guid Id { get; set; }
    public IndiaAssetCategory Category { get; set; }
    public string BankOrPlatform { get; set; } = string.Empty;
    public NriAccountType AccountType { get; set; }
    public RepatriationStatus Repatriation { get; set; }
    public string? Nominee { get; set; }

    /// <summary>
    /// Server-derived flag: <c>true</c> when the asset's account type is NRO
    /// (interest taxable in India under FEMA/TDS rules).
    /// </summary>
    public bool IsTaxableInIndia { get; set; }

    /// <summary>
    /// Server-derived flag: <c>true</c> when the asset's account type is NRE
    /// (principal and interest freely repatriable abroad).
    /// </summary>
    public bool IsRepatriable { get; set; }
}
