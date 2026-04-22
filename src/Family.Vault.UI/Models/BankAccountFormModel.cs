using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Bank Accounts add/edit dialog.</summary>
public sealed class BankAccountFormModel
{
    /// <summary>Server-assigned identifier; empty when the record is new.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Bank name is required.")]
    [MaxLength(150, ErrorMessage = "Bank name must not exceed 150 characters.")]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required.")]
    public BankAccountType AccountType { get; set; } = BankAccountType.Savings;

    [Required(ErrorMessage = "Account number is required.")]
    [MaxLength(50, ErrorMessage = "Account number must not exceed 50 characters.")]
    public string AccountNumber { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Nominee name must not exceed 100 characters.")]
    public string? Nominee { get; set; }

    /// <summary>Whether the form represents a new record (add) vs an existing one (edit).</summary>
    public bool IsNew => Id == Guid.Empty;
}

/// <summary>Read-only display model populated from the API response.</summary>
public sealed class BankAccountDisplayModel
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public BankAccountType AccountType { get; set; }

    /// <summary>Masked account number returned by the API (e.g. "••••••••1234").</summary>
    public string MaskedAccountNumber { get; set; } = string.Empty;

    public string? Nominee { get; set; }
}
