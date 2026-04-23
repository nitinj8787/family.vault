using Family.Vault.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Storage;

/// <summary>
/// Development-only implementation of <see cref="IStorageService"/> that stores files on
/// the local file system.  Replaces <see cref="BlobStorageService"/> when running locally
/// without an Azure subscription.
/// </summary>
/// <remarks>
/// <b>Do not use in production.</b>  Enable by setting <c>LocalDev:Enabled = true</c> in
/// <c>appsettings.Development.json</c> and configuring <c>LocalDev:StoragePath</c>.
/// </remarks>
public sealed class LocalFileStorageService : IStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(string storagePath, ILogger<LocalFileStorageService> logger)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path is required.", nameof(storagePath));

        _storagePath = storagePath;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Directory.CreateDirectory(_storagePath);
    }

    /// <inheritdoc/>
    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        ArgumentNullException.ThrowIfNull(content);

        var filePath = GetFilePath(fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _logger.LogInformation("LocalFileStorage: uploading {FileName} to {FilePath}", fileName, filePath);

        await using var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("LocalFileStorage: uploaded {FileName} successfully", fileName);
    }

    /// <inheritdoc/>
    public async Task DownloadAsync(string fileName, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var filePath = GetFilePath(fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("LocalFileStorage: file {FileName} not found at {FilePath}", fileName, filePath);
            throw new FileNotFoundException($"File '{fileName}' was not found in local storage.", fileName);
        }

        _logger.LogInformation("LocalFileStorage: downloading {FileName} from {FilePath}", fileName, filePath);

        await using var fileStream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await fileStream.CopyToAsync(destination, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_storagePath))
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());

        var files = Directory
            .GetFiles(_storagePath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_storagePath, f).Replace('\\', '/'))
            .ToList();

        _logger.LogInformation("LocalFileStorage: listed {Count} file(s)", files.Count);
        return Task.FromResult<IReadOnlyCollection<string>>(files);
    }

    private string GetFilePath(string fileName) =>
        Path.Combine(_storagePath, fileName.Replace('/', Path.DirectorySeparatorChar));
}
