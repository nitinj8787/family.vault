namespace Family.Vault.API.Authorization;

/// <summary>
/// Named authorization policy identifiers referenced by <c>[Authorize(Policy = …)]</c> attributes.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires the caller to hold the <see cref="Roles.Admin"/> or <see cref="Roles.FamilyUser"/> role.
    /// Used for listing and uploading vault items.
    /// </summary>
    public const string FamilyMember = "FamilyMember";

    /// <summary>
    /// Requires the caller to hold at least one of <see cref="Roles.Admin"/>,
    /// <see cref="Roles.FamilyUser"/>, or <see cref="Roles.EmergencyAccess"/>.
    /// Used for downloading vault items, where emergency responders also need read access.
    /// </summary>
    public const string VaultReader = "VaultReader";

    /// <summary>
    /// Requires the caller to hold the <see cref="Roles.Admin"/> role exclusively.
    /// Reserved for privileged administrative operations.
    /// </summary>
    public const string AdminOnly = "AdminOnly";
}
