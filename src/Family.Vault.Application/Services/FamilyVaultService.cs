using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Models;

namespace Family.Vault.Application.Services;

public sealed class FamilyVaultService(IStorageService storageService) : IFamilyVaultService
{
    public Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default) =>
        storageService.UploadAsync(fileName, content, cancellationToken);

    public Task DownloadAsync(string fileName, Stream destination, CancellationToken cancellationToken = default) =>
        storageService.DownloadAsync(fileName, destination, cancellationToken);

    public async Task<IReadOnlyCollection<VaultItemResponse>> GetVaultItemsAsync(CancellationToken cancellationToken = default)
    {
        var names = await storageService.GetFileNamesAsync(cancellationToken);
        return names.Select(name => new VaultItemResponse(name)).ToArray();
    }
}
