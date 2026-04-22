namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO returned when listing documents stored in the Family Vault.
/// </summary>
/// <param name="Id">Unique document identifier assigned at upload time.</param>
/// <param name="FileName">Original file name including extension (e.g. <c>passport.pdf</c>).</param>
/// <param name="Category">
/// Logical category under which the document is stored (e.g. <c>uk</c>, <c>india</c>, <c>insurance</c>).
/// </param>
/// <param name="FileSizeBytes">Size of the stored file in bytes.</param>
/// <param name="StoragePath">Relative blob path used in storage (e.g. <c>uk/passport.pdf</c>).</param>
/// <param name="UploadedAtUtc">UTC timestamp at which the document was uploaded.</param>
public sealed record DocumentMetadataResponse(
    Guid Id,
    string FileName,
    string Category,
    long FileSizeBytes,
    string StoragePath,
    DateTimeOffset UploadedAtUtc);
