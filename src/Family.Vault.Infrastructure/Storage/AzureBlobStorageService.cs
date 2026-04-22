using Azure.Storage.Blobs;
using Family.Vault.Application.Abstractions;

namespace Family.Vault.Infrastructure.Storage;

public sealed class AzureBlobStorageService(string connectionString, string containerName) : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient =
        new(connectionString, containerName);

    public async Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var files = new List<string>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            files.Add(blobItem.Name);
        }

        return files;
    }
}
