namespace Family.Vault.Domain.Enums;

/// <summary>
/// Represents the family-vault document categories used to organise files in storage.
/// Values are persisted as lowercase strings in the blob path prefix (e.g. <c>uk/</c>).
/// </summary>
public enum DocumentCategory
{
    /// <summary>UK-specific documents (passports, driving licences, etc.).</summary>
    Uk,

    /// <summary>India-specific documents (Aadhaar, PAN, etc.).</summary>
    India,

    /// <summary>Insurance policies and related documents.</summary>
    Insurance,

    /// <summary>Legal documents (wills, power of attorney, contracts, etc.).</summary>
    Legal,

    /// <summary>Financial documents (bank statements, tax returns, investment records, etc.).</summary>
    Financial,

    /// <summary>Medical records and health-related documents.</summary>
    Medical,

    /// <summary>Documents that do not fit any other category.</summary>
    Other
}
