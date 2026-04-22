using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Property &amp; Loans add/edit dialog.</summary>
public sealed class PropertyFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Asset name is required.")]
    [MaxLength(200, ErrorMessage = "Asset name must not exceed 200 characters.")]
    public string AssetName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    public PropertyCountry Country { get; set; } = PropertyCountry.UK;

    [Required(ErrorMessage = "Ownership is required.")]
    [MaxLength(100, ErrorMessage = "Ownership must not exceed 100 characters.")]
    public string Ownership { get; set; } = string.Empty;

    public bool LoanLinked { get; set; }

    [MaxLength(300, ErrorMessage = "Documents location must not exceed 300 characters.")]
    public string? DocumentsLocation { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class PropertyDisplayModel
{
    public Guid Id { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public PropertyCountry Country { get; set; }
    public string Ownership { get; set; } = string.Empty;
    public bool LoanLinked { get; set; }
    public string? DocumentsLocation { get; set; }
}
