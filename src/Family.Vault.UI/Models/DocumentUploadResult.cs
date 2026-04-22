namespace Family.Vault.UI.Models;

/// <summary>
/// Represents the structured response returned by the API after a successful document upload.
/// </summary>
/// <param name="FileName">Original file name as supplied by the caller.</param>
/// <param name="Category">Category string under which the document was stored (e.g. "Uk").</param>
/// <param name="FileSizeBytes">Actual size of the stored file in bytes.</param>
/// <param name="StoragePath">Relative blob path used in storage (e.g. <c>uk/passport.pdf</c>).</param>
/// <param name="UploadedAtUtc">UTC timestamp recorded at the moment of upload.</param>
public sealed record DocumentUploadResult(
    string FileName,
    string Category,
    long FileSizeBytes,
    string StoragePath,
    DateTimeOffset UploadedAtUtc);
