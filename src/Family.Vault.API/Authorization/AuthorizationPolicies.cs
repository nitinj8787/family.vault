namespace Family.Vault.API.Authorization;

/// <summary>
/// Named authorization policy identifiers referenced by <c>[Authorize(Policy = …)]</c> attributes.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires full write access.
    /// Allowed roles: <see cref="Roles.PrimaryUser"/>, legacy <see cref="Roles.Admin"/>,
    /// and legacy <see cref="Roles.FamilyUser"/>.
    /// </summary>
    public const string FullAccess = "FullAccess";

    /// <summary>
    /// Requires read access for family assets.
    /// Adds <see cref="Roles.Spouse"/> to <see cref="FullAccess"/>.
    /// </summary>
    public const string FamilyAssetReader = "FamilyAssetReader";

    /// <summary>
    /// Requires read access for critical data.
    /// Adds <see cref="Roles.EmergencyAccess"/> to <see cref="FamilyAssetReader"/>.
    /// </summary>
    public const string CriticalDataReader = "CriticalDataReader";

    /// <summary>
    /// Allows limited spouse edit access plus full-access roles.
    /// </summary>
    public const string LimitedEditor = "LimitedEditor";

    // Legacy policy names preserved for compatibility.
    public const string FamilyMember = FullAccess;
    public const string VaultReader = FamilyAssetReader;

    /// <summary>
    /// Requires the caller to hold the <see cref="Roles.Admin"/> role exclusively.
    /// Reserved for privileged administrative operations.
    /// </summary>
    public const string AdminOnly = "AdminOnly";
}
