using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Services;
using Family.Vault.Infrastructure.Secrets;
using Family.Vault.Infrastructure.Storage;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Family.Vault.API.Authorization;
using Family.Vault.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ---------------------------------------------------------------------------
// Authentication – Azure AD (Microsoft Entra ID) via JwtBearer.
// Microsoft.Identity.Web validates the token signature, issuer, audience, and
// lifetime automatically, using Azure AD's OIDC discovery metadata. All
// token validation parameters are set to secure-by-default values; only the
// values loaded from the "AzureAd" configuration section are customised.
// ---------------------------------------------------------------------------
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// ---------------------------------------------------------------------------
// Authorization – role-based policies.
// Roles are carried as "roles" claims inside the Azure AD access token and are
// assigned through App Role assignments in the Azure AD portal.
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    // Any authenticated FamilyUser or Admin can list and upload vault items.
    options.AddPolicy(AuthorizationPolicies.FamilyMember, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.FamilyUser, Roles.Admin));

    // Downloads are also accessible to EmergencyAccess users.
    options.AddPolicy(AuthorizationPolicies.VaultReader, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.FamilyUser, Roles.Admin, Roles.EmergencyAccess));

    // Privileged administrative operations are restricted to Admins only.
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.Admin));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<VaultUploadOptions>(builder.Configuration.GetSection(VaultUploadOptions.SectionName));
builder.Services.Configure<DocumentOptions>(builder.Configuration.GetSection(DocumentOptions.SectionName));

// Register DefaultAzureCredential as a singleton so that token caching is shared across all
// consumers and re-authentication round-trips are minimised.
builder.Services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

builder.Services.AddScoped<IFamilyVaultService, FamilyVaultService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
// Singleton so that in-memory profiles survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IProfileService, ProfileService>();
// Singleton so that in-memory UK assets survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IUkAssetService, UkAssetService>();
// Singleton so that in-memory India assets survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IIndiaAssetService, IndiaAssetService>();
// Singleton so that in-memory bank accounts survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IBankAccountService, BankAccountService>();
// Singleton so that in-memory investments survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IInvestmentService, InvestmentService>();
// Singleton so that in-memory insurance policies survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IInsuranceService, InsuranceService>();
// Singleton so that in-memory properties survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IPropertyService, PropertyService>();
// Singleton so that in-memory emergency fund entries survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<IEmergencyFundService, EmergencyFundService>();
// Singleton so that in-memory nominee entries survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<INomineeService, NomineeService>();
// Singleton so that in-memory tax entries survive across requests.
// Replace with a scoped, DB-backed implementation for production.
builder.Services.AddSingleton<ITaxService, TaxService>();
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

// Authentication must come before Authorization in the middleware pipeline.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
