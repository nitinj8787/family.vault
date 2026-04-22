namespace Family.Vault.UI.Models;

/// <summary>
/// UI display model for a document stored in the Document Vault.
/// Mirrors <c>DocumentMetadataResponse</c> from the API.
/// </summary>
public sealed class DocumentMetadataModel
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; }
}
