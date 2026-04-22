using Azure.Core;
using Azure.Identity;
using Family.Vault.API.Configuration;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Services;
using Family.Vault.Infrastructure.Secrets;
using Family.Vault.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<VaultUploadOptions>(builder.Configuration.GetSection(VaultUploadOptions.SectionName));

// Register DefaultAzureCredential as a singleton so that token caching is shared across all
// consumers and re-authentication round-trips are minimised.
builder.Services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

builder.Services.AddScoped<IFamilyVaultService, FamilyVaultService>();
builder.Services.AddSingleton<IStorageService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var credential = serviceProvider.GetRequiredService<TokenCredential>();
    var logger = serviceProvider.GetRequiredService<ILogger<BlobStorageService>>();

    var accountUriString = configuration["AzureStorage:AccountUri"];
    var containerName = configuration["AzureStorage:ContainerName"];

    if (string.IsNullOrWhiteSpace(accountUriString) || string.IsNullOrWhiteSpace(containerName))
    {
        throw new InvalidOperationException(
            "AzureStorage configuration is missing required values (AccountUri, ContainerName).");
    }

    if (!Uri.TryCreate(accountUriString, UriKind.Absolute, out var accountUri))
    {
        throw new InvalidOperationException($"AzureStorage:AccountUri '{accountUriString}' is not a valid absolute URI.");
    }

    return new BlobStorageService(accountUri, containerName, credential, logger);
});

builder.Services.AddSingleton<IKeyVaultService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var credential = serviceProvider.GetRequiredService<TokenCredential>();
    var logger = serviceProvider.GetRequiredService<ILogger<KeyVaultService>>();

    var vaultUriString = configuration["AzureKeyVault:VaultUri"];

    if (string.IsNullOrWhiteSpace(vaultUriString))
    {
        throw new InvalidOperationException(
            "AzureKeyVault configuration is missing required value (VaultUri).");
    }

    if (!Uri.TryCreate(vaultUriString, UriKind.Absolute, out var vaultUri))
    {
        throw new InvalidOperationException($"AzureKeyVault:VaultUri '{vaultUriString}' is not a valid absolute URI.");
    }

    return new KeyVaultService(vaultUri, credential, logger);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

