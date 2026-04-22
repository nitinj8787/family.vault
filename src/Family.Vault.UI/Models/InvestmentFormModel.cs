using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Investments add/edit dialog.</summary>
public sealed class InvestmentFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Platform is required.")]
    [MaxLength(150, ErrorMessage = "Platform must not exceed 150 characters.")]
    public string Platform { get; set; } = string.Empty;

    [Required(ErrorMessage = "Investment type is required.")]
    public InvestmentType Type { get; set; } = InvestmentType.Stocks;

    [Required(ErrorMessage = "Account ID is required.")]
    [MaxLength(50, ErrorMessage = "Account ID must not exceed 50 characters.")]
    public string AccountId { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Nominee name must not exceed 100 characters.")]
    public string? Nominee { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class InvestmentDisplayModel
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public InvestmentType Type { get; set; }

    /// <summary>Masked account / folio ID returned by the API (e.g. "••••••••1234").</summary>
    public string MaskedAccountId { get; set; } = string.Empty;

    public string? Nominee { get; set; }
}
