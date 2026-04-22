using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Family.Vault.Application.Services;

/// <summary>
/// Application-layer service that validates, categorises, stores family documents, and
/// maintains an in-memory metadata registry so documents can be listed and downloaded.
/// </summary>
/// <remarks>
/// Validation rules (max size, allowed extensions) are driven by <see cref="DocumentOptions"/>
/// so they can be adjusted per environment without code changes.
/// Documents are stored under a category-based prefix (e.g. <c>uk/passport.pdf</c>) to
/// keep the blob container organised.
/// The metadata dictionary is an in-memory stand-in for a persistent database.
/// Replace with a scoped, DB-backed store for production use.
/// </remarks>
public sealed class DocumentService(
    IStorageService storageService,
    IOptions<DocumentOptions> options,
    ILogger<DocumentService> logger) : IDocumentService
{
    private readonly DocumentOptions _options = options.Value;

    // In-memory metadata store keyed by (userId, documentId).
    private readonly ConcurrentDictionary<(string UserId, Guid DocId), DocumentMetadata> _metadataStore =
        new();

    /// <inheritdoc/>
    public async Task<DocumentUploadResponse> UploadAsync(
        string userId,
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        // Build a category-namespaced blob path so documents are organised in storage.
        var categoryPrefix = request.Category.ToString().ToLowerInvariant();
        var storagePath = $"{categoryPrefix}/{request.FileName}";

        logger.LogInformation(
            "Uploading document {FileName} (userId={UserId}, category={Category}, size={FileSizeBytes} bytes) to path {StoragePath}",
            LogSanitizer.Sanitize(request.FileName),
            userId,
            request.Category,
            request.FileSizeBytes,
            LogSanitizer.Sanitize(storagePath));

        await storageService.UploadAsync(storagePath, request.Content, cancellationToken);

        var uploadedAt = DateTimeOffset.UtcNow;

        // Persist metadata so the document appears in listings and can be downloaded by ID.
        var id = Guid.NewGuid();
        var metadata = new DocumentMetadata(
            id,
            userId,
            request.FileName,
            request.Category.ToString(),
            request.FileSizeBytes,
            storagePath,
            uploadedAt);

        _metadataStore[(userId, id)] = metadata;

        logger.LogInformation(
            "Document {FileName} (id={DocId}) successfully uploaded to {StoragePath} at {UploadedAtUtc}",
            LogSanitizer.Sanitize(request.FileName),
            id,
            LogSanitizer.Sanitize(storagePath),
            uploadedAt);

        return new DocumentUploadResponse(
            FileName: request.FileName,
            Category: request.Category,
            FileSizeBytes: request.FileSizeBytes,
            StoragePath: storagePath,
            UploadedAtUtc: uploadedAt);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DocumentMetadataResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var docs = _metadataStore.Values
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.UploadedAtUtc)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<DocumentMetadataResponse>>(docs);
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAsync(
        string userId,
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (!_metadataStore.TryGetValue((userId, id), out var metadata))
        {
            logger.LogWarning(
                "Download requested for non-existent document {DocId} for user {UserId}", id, userId);
            throw new FileNotFoundException($"Document '{id}' was not found.");
        }

        logger.LogInformation(
            "Downloading document {DocId} ({FileName}) from {StoragePath} for user {UserId}",
            id, LogSanitizer.Sanitize(metadata.FileName), LogSanitizer.Sanitize(metadata.StoragePath), userId);

        await storageService.DownloadAsync(metadata.StoragePath, destination, cancellationToken);
        return metadata.FileName;
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

    private static DocumentMetadataResponse MapToResponse(DocumentMetadata m) =>
        new(
            Id: m.Id,
            FileName: m.FileName,
            Category: m.Category,
            FileSizeBytes: m.FileSizeBytes,
            StoragePath: m.StoragePath,
            UploadedAtUtc: m.UploadedAtUtc);
}
