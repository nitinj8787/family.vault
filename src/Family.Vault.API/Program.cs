using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Services;
using Family.Vault.Infrastructure.Database;
using Family.Vault.Infrastructure.Secrets;
using Family.Vault.Infrastructure.Storage;
using Microsoft.Identity.Web;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Family.Vault.API.Authorization;
using Family.Vault.API.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ---------------------------------------------------------------------------
// Local-dev mode flag.
// When LocalDev:Enabled = true (set in appsettings.Development.json) the app
// replaces all Azure-dependent services (Azure AD auth, Blob Storage, Key Vault)
// with lightweight local alternatives so that the application can run end-to-end
// on a developer workstation without any Azure subscription.
// ---------------------------------------------------------------------------
var isLocalDev = builder.Configuration.GetValue<bool>("LocalDev:Enabled");

if (isLocalDev)
{
    // ---------------------------------------------------------------------------
    // Authentication – local dev auto-auth handler.
    // Every request is automatically authenticated with the synthetic user identity
    // defined in LocalDev:UserId / LocalDev:Roles configuration values.
    // ---------------------------------------------------------------------------
    builder.Services.AddAuthentication("LocalDev")
        .AddScheme<AuthenticationSchemeOptions, LocalDevAuthHandler>("LocalDev", null);
}
else
{
    // ---------------------------------------------------------------------------
    // Authentication – Azure AD (Microsoft Entra ID) via JwtBearer.
    // Microsoft.Identity.Web validates the token signature, issuer, audience, and
    // lifetime automatically, using Azure AD's OIDC discovery metadata. All
    // token validation parameters are set to secure-by-default values; only the
    // values loaded from the "AzureAd" configuration section are customised.
    // ---------------------------------------------------------------------------
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}

// ---------------------------------------------------------------------------
// Authorization – role-based policies.
// Roles are carried as "roles" claims inside the Azure AD access token and are
// assigned through App Role assignments in the Azure AD portal.
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    // Primary account users retain full access. Legacy roles are also supported.
    options.AddPolicy(AuthorizationPolicies.FullAccess, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.PrimaryUser, Roles.Admin, Roles.FamilyUser));

    // Spouse can read all family assets but does not get destructive permissions.
    options.AddPolicy(AuthorizationPolicies.FamilyAssetReader, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.PrimaryUser, Roles.Spouse, Roles.Admin, Roles.FamilyUser));

    // Emergency access is restricted to designated critical read endpoints.
    options.AddPolicy(AuthorizationPolicies.CriticalDataReader, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(
                  Roles.PrimaryUser,
                  Roles.Spouse,
                  Roles.EmergencyAccess,
                  Roles.Admin,
                  Roles.FamilyUser));

    // Spouse limited edit policy for non-destructive profile updates.
    options.AddPolicy(AuthorizationPolicies.LimitedEditor, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(Roles.PrimaryUser, Roles.Spouse, Roles.Admin, Roles.FamilyUser));

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
builder.Services.Configure<ReadinessScoreOptions>(builder.Configuration.GetSection(ReadinessScoreOptions.SectionName));

// ---------------------------------------------------------------------------
// SQLite — connection factory and database initialisation.
// The connection string is read from "Sqlite:ConnectionString" in configuration.
// ---------------------------------------------------------------------------
var sqliteConnectionString = builder.Configuration["Sqlite:ConnectionString"]
    ?? throw new InvalidOperationException(
        "SQLite configuration is missing required value (Sqlite:ConnectionString).");

builder.Services.AddDbContext<FamilyVaultDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));

// ---------------------------------------------------------------------------
// Service registrations – SQLite-backed implementations.
// All asset-domain services are registered as Scoped and use the request-scoped DbContext.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IProfileService, SqliteProfileService>();
builder.Services.AddScoped<IUkAssetService, SqliteUkAssetService>();
builder.Services.AddScoped<IIndiaAssetService, SqliteIndiaAssetService>();
builder.Services.AddScoped<IBankAccountService, SqliteBankAccountService>();
builder.Services.AddScoped<IInvestmentService, SqliteInvestmentService>();
builder.Services.AddScoped<IInsuranceService, SqliteInsuranceService>();
builder.Services.AddScoped<IPropertyService, SqlitePropertyService>();
builder.Services.AddScoped<IEmergencyFundService, SqliteEmergencyFundService>();
builder.Services.AddScoped<INomineeService, SqliteNomineeService>();
builder.Services.AddScoped<ITaxService, SqliteTaxService>();
builder.Services.AddScoped<IWillsService, SqliteWillsService>();

builder.Services.AddScoped<IDocumentService, SqliteDocumentService>();
builder.Services.AddScoped<IFamilyVaultService, FamilyVaultService>();
builder.Services.AddScoped<IInsightService, InsightService>();
builder.Services.AddScoped<IReadinessScoreService, ReadinessScoreService>();

if (isLocalDev)
{
    // ---------------------------------------------------------------------------
    // Local dev storage – files are written to a folder on the local machine.
    // Configure the root folder via LocalDev:StoragePath in appsettings.Development.json.
    // Defaults to a "local-storage" subfolder next to the running executable.
    // ---------------------------------------------------------------------------
    builder.Services.AddSingleton<IStorageService>(serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<LocalFileStorageService>>();
        var storagePath = configuration["LocalDev:StoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "local-storage");
        return new LocalFileStorageService(storagePath, logger);
    });

    // ---------------------------------------------------------------------------
    // Local dev key vault – secrets live in memory, pre-seeded from LocalDev:Secrets.
    // ---------------------------------------------------------------------------
    builder.Services.AddSingleton<IKeyVaultService>(serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<LocalKeyVaultService>>();
        return new LocalKeyVaultService(configuration, logger);
    });
}
else
{
    // ---------------------------------------------------------------------------
    // Production – Azure Blob Storage and Azure Key Vault via DefaultAzureCredential.
    // Register DefaultAzureCredential as a singleton so that token caching is shared
    // across all consumers and re-authentication round-trips are minimised.
    // ---------------------------------------------------------------------------
    builder.Services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

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
}

var app = builder.Build();

// ---------------------------------------------------------------------------
// Initialize the SQLite database schema on startup.
// All DDL statements use IF NOT EXISTS so this is safe to run on every start.
// ---------------------------------------------------------------------------
var dbLogger = app.Services.GetRequiredService<ILogger<Program>>();
await DatabaseInitializer.InitializeAsync(sqliteConnectionString, dbLogger);

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
