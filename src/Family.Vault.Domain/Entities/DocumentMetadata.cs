namespace Family.Vault.Domain.Entities;

/// <summary>
/// In-memory metadata record for a document uploaded to the Family Vault.
/// Stores the fields needed to list documents and reconstruct the storage path for downloads.
/// Replace with a DB-backed entity for production use.
/// </summary>
public sealed class DocumentMetadata
{
    public DocumentMetadata(
        Guid id,
        string userId,
        string fileName,
        string category,
        string description,
        long fileSizeBytes,
        string storagePath,
        DateTimeOffset uploadedAtUtc)
    {
        Id = id;
        UserId = userId;
        FileName = fileName;
        Category = category;
        Description = description;
        FileSizeBytes = fileSizeBytes;
        StoragePath = storagePath;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; }
    public string UserId { get; }
    public string FileName { get; }

    /// <summary>Category label (e.g. "uk", "india", "insurance").</summary>
    public string Category { get; }

    /// <summary>Optional free-text description supplied by the user at upload time.</summary>
    public string Description { get; }

    public long FileSizeBytes { get; }

    /// <summary>Blob path used by the storage service, e.g. <c>uk/passport.pdf</c>.</summary>
    public string StoragePath { get; }

    public DateTimeOffset UploadedAtUtc { get; }
}
