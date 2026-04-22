using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Storage;

/// <summary>
/// Production-ready Azure Blob Storage implementation of <see cref="IStorageService"/>.
/// Authenticates using the provided <see cref="TokenCredential"/> (e.g. <c>DefaultAzureCredential</c>
/// for Managed Identity / Azure AD) — no connection strings are used.
/// </summary>
public sealed class BlobStorageService : IStorageService
{
    // Prevent unbounded responses for very large containers in this starter implementation.
    private const int MaxReturnedFiles = 1000;

    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    // Double-checked locking: container creation only happens once per process lifetime.
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _containerInitialized;

    /// <param name="accountUri">
    ///   Full URI of the storage account, e.g. <c>https://&lt;account&gt;.blob.core.windows.net</c>.
    /// </param>
    /// <param name="containerName">Target blob container name.</param>
    /// <param name="credential">
    ///   Azure AD token credential (e.g. <c>DefaultAzureCredential</c>).
    ///   Register as a singleton in DI to benefit from token caching.
    /// </param>
    /// <param name="logger">Structured logger injected by the DI container.</param>
    public BlobStorageService(
        Uri accountUri,
        string containerName,
        TokenCredential credential,
        ILogger<BlobStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(accountUri);
        ArgumentNullException.ThrowIfNull(credential);

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name is required.", nameof(containerName));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var serviceClient = new BlobServiceClient(accountUri, credential);
        _containerClient = serviceClient.GetBlobContainerClient(containerName);
    }

    /// <inheritdoc/>
    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var sanitizedFileName = SanitizeFileName(fileName);

        _logger.LogInformation("Uploading blob {BlobName} to container {Container}",
            LogSanitizer.Sanitize(sanitizedFileName), _containerClient.Name);

        try
        {
            await EnsureContainerCreatedAsync(cancellationToken);
            var blobClient = _containerClient.GetBlobClient(sanitizedFileName);
            await blobClient.UploadAsync(content, overwrite: true, cancellationToken);

            _logger.LogInformation("Successfully uploaded blob {BlobName}", LogSanitizer.Sanitize(sanitizedFileName));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Storage request failed while uploading blob {BlobName}. Status: {Status}",
                LogSanitizer.Sanitize(sanitizedFileName), ex.Status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DownloadAsync(string fileName, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var sanitizedFileName = SanitizeFileName(fileName);

        _logger.LogInformation("Downloading blob {BlobName} from container {Container}",
            LogSanitizer.Sanitize(sanitizedFileName), _containerClient.Name);

        try
        {
            var blobClient = _containerClient.GetBlobClient(sanitizedFileName);
            await blobClient.DownloadToAsync(destination, cancellationToken);

            _logger.LogInformation("Successfully downloaded blob {BlobName}", LogSanitizer.Sanitize(sanitizedFileName));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob {BlobName} was not found in container {Container}",
                LogSanitizer.Sanitize(sanitizedFileName), _containerClient.Name);
            throw new FileNotFoundException($"Blob '{sanitizedFileName}' was not found.", sanitizedFileName, ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Storage request failed while downloading blob {BlobName}. Status: {Status}",
                LogSanitizer.Sanitize(sanitizedFileName), ex.Status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing blobs in container {Container}", _containerClient.Name);

        try
        {
            await EnsureContainerCreatedAsync(cancellationToken);

            var files = new List<string>();

            await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                files.Add(blobItem.Name);

                if (files.Count >= MaxReturnedFiles)
                {
                    _logger.LogWarning("Blob listing truncated at {Limit} items for container {Container}",
                        MaxReturnedFiles, _containerClient.Name);
                    break;
                }
            }

            _logger.LogInformation("Listed {Count} blobs in container {Container}", files.Count, _containerClient.Name);
            return files;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Storage request failed while listing blobs. Status: {Status}", ex.Status);
            throw;
        }
    }

    private async Task EnsureContainerCreatedAsync(CancellationToken cancellationToken)
    {
        // Fast path — container already confirmed to exist.
        if (_containerInitialized)
        {
            return;
        }

        // Guard container creation with a lock so concurrent requests do not issue
        // redundant CreateIfNotExists network calls during startup.
        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_containerInitialized)
            {
                return;
            }

            _logger.LogInformation("Ensuring blob container {Container} exists", _containerClient.Name);
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            _containerInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var sanitized = Path.GetFileName(fileName);

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ArgumentException("File name is invalid.", nameof(fileName));
        }

        return sanitized;
    }
}