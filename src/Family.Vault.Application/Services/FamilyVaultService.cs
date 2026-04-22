using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Models;

namespace Family.Vault.Application.Services;

public sealed class FamilyVaultService(IBlobStorageService blobStorageService) : IFamilyVaultService
{
    public Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default) =>
        blobStorageService.UploadAsync(fileName, content, cancellationToken);

    public async Task<IReadOnlyCollection<VaultItemResponse>> GetVaultItemsAsync(CancellationToken cancellationToken = default)
    {
        var names = await blobStorageService.GetFileNamesAsync(cancellationToken);
        return names.Select(name => new VaultItemResponse(name)).ToArray();
    }
}
