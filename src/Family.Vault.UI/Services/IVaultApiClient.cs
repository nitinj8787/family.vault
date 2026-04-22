using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// Abstraction for communicating with the FamilyVault REST API.
/// Implementations are responsible for attaching bearer tokens to each request.
/// </summary>
public interface IVaultApiClient
{
    /// <summary>Returns the list of files currently stored in the vault.</summary>
    Task<IReadOnlyList<VaultFileItem>> GetVaultItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a document to the vault under the specified category.
    /// </summary>
    /// <param name="fileName">Original file name including extension.</param>
    /// <param name="fileSizeBytes">File size in bytes (used for client-side logging).</param>
    /// <param name="content">Readable stream with the file bytes.</param>
    /// <param name="category">Category name: <c>Uk</c>, <c>India</c>, or <c>Insurance</c>.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<DocumentUploadResult> UploadDocumentAsync(
        string fileName,
        long fileSizeBytes,
        Stream content,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the rich metadata list for all documents belonging to the signed-in user.
    /// </summary>
    Task<IReadOnlyList<DocumentMetadataModel>> GetDocumentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the document identified by <paramref name="id"/> and returns its raw bytes
    /// together with the original file name.
    /// </summary>
    Task<(byte[] Bytes, string FileName)> DownloadDocumentAsync(
        Guid id,
        string fileName,
        CancellationToken cancellationToken = default);
}
