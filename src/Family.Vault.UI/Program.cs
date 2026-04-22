using Family.Vault.UI.Components;
using Family.Vault.UI.Services;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ---------------------------------------------------------------------------
// Authentication – Azure AD (Entra ID) OIDC for interactive users.
// Token acquisition is enabled so the UI can call the protected API on behalf
// of the signed-in user using the On-Behalf-Of / authorisation-code flow.
// ---------------------------------------------------------------------------
var apiScopes = builder.Configuration.GetSection("VaultApi:Scopes").Get<string[]>() ?? [];

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

// Required for AuthorizeRouteView / CascadingAuthenticationState in .NET 8.
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Token provider – selectable via VaultApi:UseAzureAdAuth (default: false).
//
//   false (default / development) → PlaceholderTokenProvider
//     Reads a static token from VaultApi:BearerToken, or sends requests
//     unauthenticated when that key is absent.  Safe for local runs against
//     an API with auth disabled.
//
//   true (production) → AzureAdTokenProvider
//     Acquires an MSAL-cached bearer token for the signed-in user via
//     ITokenAcquisition.  Requires a fully configured AzureAd section.
// ---------------------------------------------------------------------------
var useAzureAdAuth = builder.Configuration.GetValue<bool>("VaultApi:UseAzureAdAuth");

if (useAzureAdAuth)
{
    builder.Services.AddScoped<ITokenProvider, AzureAdTokenProvider>();
}
else
{
    builder.Services.AddSingleton<ITokenProvider, PlaceholderTokenProvider>();
}

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
