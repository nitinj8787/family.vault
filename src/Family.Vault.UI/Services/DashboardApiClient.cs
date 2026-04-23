using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API dashboard endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class DashboardApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<DashboardApiClient> logger) : IDashboardApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<ReadinessScoreModel> GetReadinessScoreAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching readiness score from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/score");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var score = await response.Content
            .ReadFromJsonAsync<ReadinessScoreModel>(_jsonOptions, cancellationToken)
            ?? new ReadinessScoreModel();

        logger.LogInformation("Loaded readiness score: {Score}/100", score.TotalScore);
        return score;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private async Task AttachAuthorizationAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
