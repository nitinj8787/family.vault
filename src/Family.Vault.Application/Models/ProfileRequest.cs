namespace Family.Vault.Application.Models;

/// <summary>Request payload for creating or updating a user profile.</summary>
/// <param name="FullName">Primary account holder's full name.</param>
/// <param name="DateOfBirth">Optional date of birth.</param>
/// <param name="Address">Full postal address.</param>
/// <param name="SpouseName">Spouse or partner's name.</param>
/// <param name="Children">List of children's details.</param>
/// <param name="EmergencyContacts">List of emergency contacts.</param>
public sealed record ProfileRequest(
    string FullName,
    DateOnly? DateOfBirth,
    string? Address,
    string? SpouseName,
    IReadOnlyList<ChildDetailDto> Children,
    IReadOnlyList<EmergencyContactDto> EmergencyContacts);
