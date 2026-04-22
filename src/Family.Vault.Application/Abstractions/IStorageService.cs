namespace Family.Vault.Application.Abstractions;

public interface IStorageService
{
    /// <summary>Uploads a file stream to blob storage, overwriting any existing blob with the same name.</summary>
    Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Downloads a blob and writes its content to the provided <paramref name="destination"/> stream.</summary>
    Task DownloadAsync(string fileName, Stream destination, CancellationToken cancellationToken = default);

    /// <summary>Returns the names of all blobs in the container (up to the internal page limit).</summary>
    Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default);
}
