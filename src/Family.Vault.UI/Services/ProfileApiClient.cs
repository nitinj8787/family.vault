using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API profile endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>, keeping this class
/// independent of the underlying identity mechanism.
/// </summary>
public sealed class ProfileApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<ProfileApiClient> logger) : IProfileApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<ProfileFormModel?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching profile from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/profile");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            logger.LogInformation("No profile found; returning null");
            return null;
        }

        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<ProfileFormModel>(
            _jsonOptions, cancellationToken);

        logger.LogInformation("Profile loaded successfully");
        return model;
    }

    /// <inheritdoc/>
    public async Task<ProfileFormModel> SaveProfileAsync(
        ProfileFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving profile to API");

        using var request = new HttpRequestMessage(HttpMethod.Put, "api/profile")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Profile save failed (HTTP {StatusCode}): {Error}",
                (int)response.StatusCode, error);

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(error) ? $"Save failed ({(int)response.StatusCode})." : error,
                inner: null,
                response.StatusCode);
        }

        var saved = await response.Content.ReadFromJsonAsync<ProfileFormModel>(
            _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after saving profile.");

        logger.LogInformation("Profile saved successfully");
        return saved;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Acquires a bearer token from <see cref="ITokenProvider"/> and attaches it to
    /// <paramref name="request"/> as a per-request Authorization header.
    /// No header is set when the provider returns an empty or null token (e.g. the
    /// placeholder in dev when no static token is configured).
    /// </summary>
    private async Task AttachAuthorizationAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
