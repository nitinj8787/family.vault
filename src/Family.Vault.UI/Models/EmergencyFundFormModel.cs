using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Emergency Fund add/edit dialog.</summary>
public sealed class EmergencyFundFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [MaxLength(150, ErrorMessage = "Location must not exceed 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Amount must be zero or greater.")]
    public decimal Amount { get; set; }

    [MaxLength(500, ErrorMessage = "Access instructions must not exceed 500 characters.")]
    public string? AccessInstructions { get; set; }

    /// <summary>
    /// Optional monthly-expenses figure entered by the user for the
    /// "months of cover" comparison shown on the summary banner.
    /// Not persisted to the API.
    /// </summary>
    public decimal? MonthlyExpenses { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class EmergencyFundDisplayModel
{
    public Guid Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? AccessInstructions { get; set; }
}
