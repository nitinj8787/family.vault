namespace Family.Vault.Application.Configuration;

/// <summary>
/// Configuration options that govern document upload validation in <c>DocumentService</c>.
/// Bind to the <c>Document</c> section of application settings.
/// </summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Document";

    /// <summary>Maximum permitted file size in bytes. Defaults to 20 MB.</summary>
    public long MaxFileSizeBytes { get; init; } = 20 * 1024 * 1024;

    /// <summary>
    /// File extensions (including the leading dot) that are accepted for upload.
    /// Comparison is case-insensitive. Defaults to common document and image formats.
    /// </summary>
    public string[] AllowedExtensions { get; init; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".txt", ".docx"];
}
