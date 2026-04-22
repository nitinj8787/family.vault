using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Provides document management operations: validation, category assignment, storage, and metadata retrieval.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Validates <paramref name="request"/>, derives the storage path from the assigned
    /// category, uploads the content via the underlying storage service, persists document
    /// metadata for later listing and download, and returns a structured
    /// <see cref="DocumentUploadResponse"/>.
    /// </summary>
    /// <param name="userId">Identity of the user performing the upload.</param>
    /// <exception cref="Exceptions.DocumentValidationException">
    /// Thrown when the file size exceeds the configured maximum or the file extension is
    /// not in the allowed list.
    /// </exception>
    Task<DocumentUploadResponse> UploadAsync(
        string userId,
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all document metadata records belonging to <paramref name="userId"/>.
    /// </summary>
    Task<IReadOnlyList<DocumentMetadataResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the document identified by <paramref name="id"/> for <paramref name="userId"/>
    /// and writes its bytes to <paramref name="destination"/>.
    /// </summary>
    /// <returns>
    /// The original file name, so the caller can set an appropriate
    /// <c>Content-Disposition</c> header.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when no document with <paramref name="id"/> exists for the user.
    /// </exception>
    Task<string> DownloadAsync(
        string userId,
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default);
}
