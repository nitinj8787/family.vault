namespace Family.Vault.API.Authorization;

/// <summary>
/// Application role names. These must match the role claims issued by Azure AD
/// (App Role display names configured in the Azure AD app registration manifest).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string FamilyUser = "FamilyUser";
    public const string EmergencyAccess = "EmergencyAccess";
}
