using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Wills &amp; Legal add/edit dialog.</summary>
public sealed class WillFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Country is required.")]
    [MaxLength(100, ErrorMessage = "Country must not exceed 100 characters.")]
    public string Country { get; set; } = string.Empty;

    public bool WillExists { get; set; }

    [MaxLength(200, ErrorMessage = "Location must not exceed 200 characters.")]
    public string? Location { get; set; }

    [MaxLength(150, ErrorMessage = "Executor name must not exceed 150 characters.")]
    public string? Executor { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class WillDisplayModel
{
    public Guid Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool WillExists { get; set; }
    public string? Location { get; set; }
    public string? Executor { get; set; }

    /// <summary>
    /// True when no will exists for this jurisdiction.
    /// Drives the CRITICAL warning UI.
    /// </summary>
    public bool IsMissingWill => !WillExists;
}
