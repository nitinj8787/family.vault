using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Carries the data required to upload a single document through <c>IDocumentService</c>.
/// </summary>
/// <param name="FileName">Original file name including extension (e.g. <c>passport.pdf</c>).</param>
/// <param name="FileSizeBytes">Size of the content stream in bytes, used for pre-validation.</param>
/// <param name="Content">Readable stream containing the file bytes.</param>
/// <param name="Category">Logical category used to organise the document in storage.</param>
public sealed record DocumentUploadRequest(
    string FileName,
    long FileSizeBytes,
    Stream Content,
    DocumentCategory Category);
