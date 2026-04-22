using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Nominee Manager add/edit dialog.</summary>
public sealed class NomineeFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Asset type is required.")]
    public string AssetType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Institution is required.")]
    [MaxLength(150, ErrorMessage = "Institution must not exceed 150 characters.")]
    public string Institution { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nominee name is required.")]
    [MaxLength(150, ErrorMessage = "Nominee name must not exceed 150 characters.")]
    public string NomineeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationship is required.")]
    [MaxLength(100, ErrorMessage = "Relationship must not exceed 100 characters.")]
    public string Relationship { get; set; } = string.Empty;

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class NomineeDisplayModel
{
    public Guid Id { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string NomineeName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}
