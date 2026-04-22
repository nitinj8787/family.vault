using Family.Vault.Domain.ValueObjects;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents the personal profile of a Family Vault user, including
/// family details and emergency contacts.
/// </summary>
public sealed record UserProfile
{
    public required string FullName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Address { get; init; }
    public string? SpouseName { get; init; }
    public IReadOnlyList<ChildDetail> Children { get; init; } = [];
    public IReadOnlyList<EmergencyContact> EmergencyContacts { get; init; } = [];
}
