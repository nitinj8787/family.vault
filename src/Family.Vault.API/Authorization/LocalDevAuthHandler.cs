using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Family.Vault.API.Authorization;

/// <summary>
/// Development-only ASP.NET Core authentication handler that automatically authenticates
/// every incoming request with a synthetic user identity.  Role claims are read from
/// <c>LocalDev:Roles</c> and the user identifier from <c>LocalDev:UserId</c> in
/// application configuration.
/// </summary>
/// <remarks>
/// <b>Do not use in production.</b>  This handler is registered only when
/// <c>LocalDev:Enabled = true</c> in <c>appsettings.Development.json</c>.
/// </remarks>
public sealed class LocalDevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = configuration["LocalDev:UserId"] ?? "local-dev-user";
        var roles = configuration.GetSection("LocalDev:Roles").Get<string[]>()
                    ?? ["PrimaryUser", "Admin"];

        var claims = new List<Claim>
        {
            new("oid", userId),
            new("sub", userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.NameIdentifier, userId),
        };

        foreach (var role in roles)
        {
            // "roles" is the claim name used by the Azure AD access token and by the
            // RequireRole checks wired up in Program.cs.  ClaimTypes.Role is also added
            // so that ASP.NET Core's built-in role checks resolve correctly.
            claims.Add(new Claim("roles", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
