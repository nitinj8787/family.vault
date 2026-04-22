using Family.Vault.API.Configuration;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Services;
using Family.Vault.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<VaultUploadOptions>(builder.Configuration.GetSection(VaultUploadOptions.SectionName));

builder.Services.AddScoped<IFamilyVaultService, FamilyVaultService>();
builder.Services.AddSingleton<IBlobStorageService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["AzureStorage:ConnectionString"];
    var containerName = configuration["AzureStorage:ContainerName"];

    if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
    {
        throw new InvalidOperationException("AzureStorage configuration is missing required values.");
    }

    return new AzureBlobStorageService(connectionString, containerName);
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
