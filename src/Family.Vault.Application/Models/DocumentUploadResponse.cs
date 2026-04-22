using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Structured response returned by <c>IDocumentService.UploadAsync</c> after a successful upload.
/// </summary>
/// <param name="FileName">Original file name as supplied by the caller.</param>
/// <param name="Category">Category under which the document was stored.</param>
/// <param name="FileSizeBytes">Actual size of the stored file in bytes.</param>
/// <param name="StoragePath">
/// Relative blob path used in storage (e.g. <c>uk/passport.pdf</c>).
/// Useful for constructing download URLs or audit records.
/// </param>
/// <param name="UploadedAtUtc">UTC timestamp recorded at the moment of upload.</param>
public sealed record DocumentUploadResponse(
    string FileName,
    DocumentCategory Category,
    long FileSizeBytes,
    string StoragePath,
    DateTimeOffset UploadedAtUtc);
