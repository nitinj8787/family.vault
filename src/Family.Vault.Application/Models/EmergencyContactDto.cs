namespace Family.Vault.Application.Models;

/// <summary>DTO for a single emergency contact.</summary>
/// <param name="Name">Contact's full name.</param>
/// <param name="Relationship">Relationship to the account holder (e.g. "Sister", "GP").</param>
/// <param name="PhoneNumber">Contact's phone number.</param>
public sealed record EmergencyContactDto(string Name, string Relationship, string PhoneNumber);
