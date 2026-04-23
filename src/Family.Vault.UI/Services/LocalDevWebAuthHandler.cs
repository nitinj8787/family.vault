using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Family.Vault.UI.Services;

/// <summary>
/// Development-only ASP.NET Core authentication handler for the Blazor Server UI.
/// Automatically authenticates every request with a synthetic local dev user identity so
/// that the application can be run end-to-end without an Azure AD tenant.
/// </summary>
/// <remarks>
/// <b>Do not use in production.</b>  Activated when <c>VaultApi:UseAzureAdAuth = false</c>
/// in <c>appsettings.Development.json</c>.
/// </remarks>
public sealed class LocalDevWebAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = configuration["LocalDev:UserId"] ?? "local-dev-user";
        var displayName = configuration["LocalDev:DisplayName"] ?? "Local Dev User";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName),
            new("sub", userId),
            new("oid", userId),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
