using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Family.Vault.Infrastructure.Database;

public sealed class SqliteDocumentService(
    FamilyVaultDbContext dbContext,
    IStorageService storageService,
    IOptions<DocumentOptions> options,
    ILogger<SqliteDocumentService> logger) : IDocumentService
{
    private readonly DocumentOptions _options = options.Value;

    public async Task<DocumentUploadResponse> UploadAsync(
        string userId,
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

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
        var id = Guid.NewGuid();

        dbContext.Documents.Add(new DocumentMetadataEntity
        {
            Id = id.ToString(),
            UserId = userId,
            FileName = request.FileName,
            Category = request.Category.ToString(),
            Description = request.Description,
            FileSize = request.FileSizeBytes,
            StoragePath = storagePath,
            UploadedAt = uploadedAt.UtcDateTime.ToString("o")
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DocumentUploadResponse(
            FileName: request.FileName,
            Category: request.Category,
            Description: request.Description,
            FileSizeBytes: request.FileSizeBytes,
            StoragePath: storagePath,
            UploadedAtUtc: uploadedAt);
    }

    public async Task<IReadOnlyList<DocumentMetadataResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<DocumentMetadataResponse>> SearchAsync(
        string userId,
        string? category = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category.ToLower() == category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(d => d.FileName.Contains(searchTerm) || d.Description.Contains(searchTerm));
        }

        var rows = await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToResponse).ToList();
    }

    public async Task<string> DownloadAsync(
        string userId,
        Guid id,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        var metadata = await dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                d => d.UserId == userId && d.Id == id.ToString(),
                cancellationToken);

        if (metadata is null)
        {
            logger.LogWarning("Download requested for non-existent document {DocId} for user {UserId}", id, userId);
            throw new FileNotFoundException($"Document '{id}' was not found.");
        }

        await storageService.DownloadAsync(metadata.StoragePath, destination, cancellationToken);
        return metadata.FileName;
    }

    private void ValidateRequest(DocumentUploadRequest request)
    {
        if (request.FileSizeBytes <= 0)
        {
            throw new DocumentValidationException("Document content must not be empty.");
        }

        if (request.FileSizeBytes > _options.MaxFileSizeBytes)
        {
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
            throw new DocumentValidationException(
                $"File extension '{extension}' is not permitted. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}.");
        }
    }

    private static DocumentMetadataResponse MapToResponse(DocumentMetadataEntity d) =>
        new(
            Id: Guid.Parse(d.Id),
            FileName: d.FileName,
            Category: d.Category,
            Description: d.Description,
            FileSizeBytes: d.FileSize,
            StoragePath: d.StoragePath,
            UploadedAtUtc: ParseTimestamp(d.UploadedAt));

    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (DateTimeOffset.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }
}
