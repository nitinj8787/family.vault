using Family.Vault.UI.Components;
using Family.Vault.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ---------------------------------------------------------------------------
// Token provider – selectable via VaultApi:UseAzureAdAuth (default: false).
//
//   false (default / development) → PlaceholderTokenProvider + LocalDevWebAuthHandler
//     Skips Azure AD entirely.  The UI auto-authenticates every request using a
//     synthetic local dev user and calls the API without a bearer token.
//     The API must also be running in local dev mode (LocalDev:Enabled = true).
//
//   true (production) → AzureAdTokenProvider + Microsoft Identity Web OIDC
//     Full Azure AD authentication flow for both the UI and API.
// ---------------------------------------------------------------------------
var useAzureAdAuth = builder.Configuration.GetValue<bool>("VaultApi:UseAzureAdAuth");

if (useAzureAdAuth)
{
    var apiScopes = builder.Configuration.GetSection("VaultApi:Scopes").Get<string[]>() ?? [];

    // ---------------------------------------------------------------------------
    // Authentication – Azure AD (Entra ID) OIDC for interactive users.
    // Token acquisition is enabled so the UI can call the protected API on behalf
    // of the signed-in user using the On-Behalf-Of / authorisation-code flow.
    // ---------------------------------------------------------------------------
    builder.Services
        .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
        .EnableTokenAcquisitionToCallDownstreamApi(apiScopes)
        .AddInMemoryTokenCaches();

    // Exposes /MicrosoftIdentity/Account/SignIn and SignOut controller endpoints.
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // All Blazor routes require an authenticated user by default.
    builder.Services.AddAuthorization(options =>
        options.FallbackPolicy = options.DefaultPolicy);

    builder.Services.AddScoped<ITokenProvider, AzureAdTokenProvider>();
}
else
{
    // ---------------------------------------------------------------------------
    // Authentication – local dev auto-auth handler.
    // Every request is automatically authenticated with the synthetic user identity
    // defined in LocalDev:UserId / LocalDev:DisplayName configuration values.
    // No Azure AD tenant, client ID, or credentials are required.
    // ---------------------------------------------------------------------------
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "LocalDev";
        options.DefaultAuthenticateScheme = "LocalDev";
        options.DefaultChallengeScheme = "LocalDev";
    })
    .AddScheme<AuthenticationSchemeOptions, LocalDevWebAuthHandler>("LocalDev", null);

    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // No forced auth fallback – the local dev handler authenticates automatically.
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<ITokenProvider, PlaceholderTokenProvider>();
}

// Required for AuthorizeRouteView / CascadingAuthenticationState in .NET 8.
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// Toast notification service – scoped so each circuit gets its own instance.
builder.Services.AddScoped<ToastService>();

// Typed HttpClient for the vault API. The base address is read from configuration.
builder.Services.AddHttpClient<IVaultApiClient, VaultApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the profile API — shares the same base address.
builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the UK Assets API — shares the same base address.
builder.Services.AddHttpClient<IUkAssetsApiClient, UkAssetsApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the India Assets API — shares the same base address.
builder.Services.AddHttpClient<IIndiaAssetsApiClient, IndiaAssetsApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Bank Accounts API — shares the same base address.
builder.Services.AddHttpClient<IBankAccountsApiClient, BankAccountsApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Investments API — shares the same base address.
builder.Services.AddHttpClient<IInvestmentsApiClient, InvestmentsApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Insurance API — shares the same base address.
builder.Services.AddHttpClient<IInsuranceApiClient, InsuranceApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Properties API — shares the same base address.
builder.Services.AddHttpClient<IPropertyApiClient, PropertyApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Emergency Fund API — shares the same base address.
builder.Services.AddHttpClient<IEmergencyFundApiClient, EmergencyFundApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Nominees API — shares the same base address.
builder.Services.AddHttpClient<INomineeApiClient, NomineeApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Tax API — shares the same base address.
builder.Services.AddHttpClient<ITaxApiClient, TaxApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Wills & Legal API — shares the same base address.
builder.Services.AddHttpClient<IWillsApiClient, WillsApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Typed HttpClient for the Dashboard API (insights, readiness score).
builder.Services.AddHttpClient<IDashboardApiClient, DashboardApiClient>(client =>
{
    var baseUrl = builder.Configuration["VaultApi:BaseUrl"]
        ?? throw new InvalidOperationException("VaultApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Authentication must come before Authorization in the middleware pipeline.
app.UseAuthentication();
app.UseAuthorization();

// Expose Microsoft Identity sign-in / sign-out endpoints.
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
