namespace Family.Vault.API.Configuration;

public sealed class VaultUploadOptions
{
    public const string SectionName = "VaultUpload";

    public long MaxUploadBytes { get; init; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; init; } = [".pdf", ".png", ".jpg", ".jpeg", ".txt"];
}
