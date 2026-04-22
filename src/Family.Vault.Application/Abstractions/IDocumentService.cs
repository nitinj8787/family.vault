using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

/// <summary>
/// Provides document management operations: validation, category assignment, and storage.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Validates <paramref name="request"/>, derives the storage path from the assigned
    /// category, uploads the content via the underlying storage service, and returns a
    /// structured <see cref="DocumentUploadResponse"/>.
    /// </summary>
    /// <exception cref="Exceptions.DocumentValidationException">
    /// Thrown when the file size exceeds the configured maximum or the file extension is
    /// not in the allowed list.
    /// </exception>
    Task<DocumentUploadResponse> UploadAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default);
}
