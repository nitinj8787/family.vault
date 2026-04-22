using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API investments endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class InvestmentsApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<InvestmentsApiClient> logger) : IInvestmentsApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InvestmentDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching investments from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/investments");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<InvestmentDisplayModel>>(_jsonOptions, cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} investments", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<InvestmentDisplayModel> AddAsync(
        InvestmentFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding investment ({Platform})", model.Platform);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/investments")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "add", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<InvestmentDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after adding investment.");
    }

    /// <inheritdoc/>
    public async Task<InvestmentDisplayModel> UpdateAsync(
        InvestmentFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating investment {InvestmentId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/investments/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<InvestmentDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after updating investment.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting investment {InvestmentId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/investments/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Investment {InvestmentId} deleted", id);
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

    private static async Task ThrowIfFailureAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(error)
                    ? $"Investment {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
