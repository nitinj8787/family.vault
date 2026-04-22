namespace Family.Vault.Application.Models;

/// <summary>DTO representing a child's basic details.</summary>
/// <param name="Name">The child's full name.</param>
/// <param name="DateOfBirth">Optional date of birth.</param>
public sealed record ChildDetailDto(string Name, DateOnly? DateOfBirth);
