using Family.Vault.Application.Models;

namespace Family.Vault.Application.Abstractions;

public interface IFamilyVaultService
{
    Task UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

    Task DownloadAsync(string fileName, Stream destination, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VaultItemResponse>> GetVaultItemsAsync(CancellationToken cancellationToken = default);
}
