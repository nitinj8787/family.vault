namespace Family.Vault.Domain.ValueObjects;

/// <summary>Details of a child within a family profile.</summary>
/// <param name="Name">The child's full name.</param>
/// <param name="DateOfBirth">Optional date of birth.</param>
public sealed record ChildDetail(string Name, DateOnly? DateOfBirth);
