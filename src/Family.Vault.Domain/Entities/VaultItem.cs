namespace Family.Vault.Domain.Entities;

public sealed class VaultItem
{
    public VaultItem(Guid id, string fileName, DateTimeOffset createdOnUtc)
    {
        Id = id;
        FileName = fileName;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public DateTimeOffset CreatedOnUtc { get; }
}
