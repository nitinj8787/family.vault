using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Family.Vault.Application.Services;

/// <summary>
/// Application-layer service that validates, categorises, and stores family documents.
/// </summary>
/// <remarks>
/// Validation rules (max size, allowed extensions) are driven by <see cref="DocumentOptions"/>
/// so they can be adjusted per environment without code changes.
/// Documents are stored under a category-based prefix (e.g. <c>uk/passport.pdf</c>) to
/// keep the blob container organised.
/// </remarks>
public sealed class DocumentService(
    IStorageService storageService,
    IOptions<DocumentOptions> options,
    ILogger<DocumentService> logger) : IDocumentService
{
    private readonly DocumentOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<DocumentUploadResponse> UploadAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        // Build a category-namespaced blob path so documents are organised in storage.
        var categoryPrefix = request.Category.ToString().ToLowerInvariant();
        var storagePath = $"{categoryPrefix}/{request.FileName}";

        logger.LogInformation(
            "Uploading document {FileName} (category={Category}, size={FileSizeBytes} bytes) to path {StoragePath}",
            LogSanitizer.Sanitize(request.FileName),
            request.Category,
            request.FileSizeBytes,
            LogSanitizer.Sanitize(storagePath));

        await storageService.UploadAsync(storagePath, request.Content, cancellationToken);

        var uploadedAt = DateTimeOffset.UtcNow;

        logger.LogInformation(
            "Document {FileName} successfully uploaded to {StoragePath} at {UploadedAtUtc}",
            LogSanitizer.Sanitize(request.FileName),
            LogSanitizer.Sanitize(storagePath),
            uploadedAt);

        return new DocumentUploadResponse(
            FileName: request.FileName,
            Category: request.Category,
            FileSizeBytes: request.FileSizeBytes,
            StoragePath: storagePath,
            UploadedAtUtc: uploadedAt);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private void ValidateRequest(DocumentUploadRequest request)
    {
        if (request.FileSizeBytes <= 0)
        {
            throw new DocumentValidationException("Document content must not be empty.");
        }

        if (request.FileSizeBytes > _options.MaxFileSizeBytes)
        {
            logger.LogWarning(
                "Document {FileName} rejected: size {FileSizeBytes} bytes exceeds limit of {MaxFileSizeBytes} bytes",
                LogSanitizer.Sanitize(request.FileName),
                request.FileSizeBytes,
                _options.MaxFileSizeBytes);

            throw new DocumentValidationException(
                $"File size {request.FileSizeBytes} bytes exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(extension))
        {
            throw new DocumentValidationException("File must have an extension.");
        }

        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Document {FileName} rejected: extension '{Extension}' is not allowed",
                LogSanitizer.Sanitize(request.FileName),
                extension);

            throw new DocumentValidationException(
                $"File extension '{extension}' is not permitted. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}.");
        }
    }
}
