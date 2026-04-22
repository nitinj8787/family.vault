namespace Family.Vault.Application.Abstractions;

public interface IBlobStorageService
{
    Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetFileNamesAsync(CancellationToken cancellationToken = default);
}
