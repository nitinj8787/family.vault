using Azure.Storage.Blobs;
using Family.Vault.Application.Abstractions;

namespace Family.Vault.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    // Prevent unbounded responses for very large containers in this starter implementation.
    private const int MaxReturnedFiles = 1000;

    private readonly BlobContainerClient _containerClient;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _containerInitialized;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name is required.", nameof(containerName));
        }

        _containerClient = new BlobContainerClient(connectionString, containerName);
    }

    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        await EnsureContainerCreatedAsync(cancellationToken);
        var sanitizedFileName = SanitizeFileName(fileName);
        var blobClient = _containerClient.GetBlobClient(sanitizedFileName);
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureContainerCreatedAsync(cancellationToken);

        var files = new List<string>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            files.Add(blobItem.Name);

            if (files.Count >= MaxReturnedFiles)
            {
                break;
            }
        }

        return files;
    }

    private async Task EnsureContainerCreatedAsync(CancellationToken cancellationToken)
    {
        // Guard container creation with a lock so concurrent requests do not create duplicate network calls.
        if (_containerInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_containerInitialized)
            {
                return;
            }

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

        var sanitizedFileName = Path.GetFileName(fileName);

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            sanitizedFileName = sanitizedFileName.Replace(invalidCharacter, '_');
        }

        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            throw new ArgumentException("File name is invalid.", nameof(fileName));
        }

        return sanitizedFileName;
    }
}
