using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Tax Summary add/edit dialog.</summary>
public sealed class TaxFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Income type is required.")]
    public string IncomeType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    [MaxLength(100, ErrorMessage = "Country must not exceed 100 characters.")]
    public string Country { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Tax paid must be zero or greater.")]
    public decimal TaxPaid { get; set; }

    public bool DeclaredInUk { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class TaxDisplayModel
{
    public Guid Id { get; set; }
    public string IncomeType { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TaxPaid { get; set; }
    public bool DeclaredInUk { get; set; }

    /// <summary>
    /// True when the income is from a non-UK country and has not been declared in the UK.
    /// Signals a potential undeclared-income issue.
    /// </summary>
    public bool IsUndeclaredForeignIncome =>
        !DeclaredInUk &&
        !string.Equals(Country, "United Kingdom", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Country, "UK", StringComparison.OrdinalIgnoreCase);
}
