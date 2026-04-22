using System.ComponentModel.DataAnnotations;

namespace Family.Vault.UI.Models;

/// <summary>Form binding model for the Personal &amp; Contacts profile page.</summary>
public sealed class ProfileFormModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(300, ErrorMessage = "Address must not exceed 300 characters.")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "Spouse name must not exceed 100 characters.")]
    public string? SpouseName { get; set; }

    public List<ChildFormModel> Children { get; set; } = [];

    public List<EmergencyContactFormModel> EmergencyContacts { get; set; } = [];
}

/// <summary>Form binding model for a single child entry.</summary>
public sealed class ChildFormModel
{
    [Required(ErrorMessage = "Child's name is required.")]
    [MaxLength(100, ErrorMessage = "Child's name must not exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>Form binding model for a single emergency contact entry.</summary>
public sealed class EmergencyContactFormModel
{
    [Required(ErrorMessage = "Contact name is required.")]
    [MaxLength(100, ErrorMessage = "Contact name must not exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Relationship is required.")]
    [MaxLength(100, ErrorMessage = "Relationship must not exceed 100 characters.")]
    public string Relationship { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;
}
