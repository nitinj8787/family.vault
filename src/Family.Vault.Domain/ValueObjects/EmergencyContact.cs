namespace Family.Vault.Domain.ValueObjects;

/// <summary>An emergency contact for the family vault profile.</summary>
/// <param name="Name">Contact's full name.</param>
/// <param name="Relationship">Relationship to the primary account holder (e.g. "Sister", "GP").</param>
/// <param name="PhoneNumber">Contact phone number.</param>
public sealed record EmergencyContact(string Name, string Relationship, string PhoneNumber);
