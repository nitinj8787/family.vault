using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API India-assets endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class IndiaAssetsApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<IndiaAssetsApiClient> logger) : IIndiaAssetsApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IndiaAssetDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching India assets from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/india-assets");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<IndiaAssetDisplayModel>>(_jsonOptions, cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} India assets", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<IndiaAssetDisplayModel> AddAsync(
        IndiaAssetFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding India asset ({Platform})", model.BankOrPlatform);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/india-assets")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "add", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<IndiaAssetDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after adding asset.");
    }

    /// <inheritdoc/>
    public async Task<IndiaAssetDisplayModel> UpdateAsync(
        IndiaAssetFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating India asset {AssetId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/india-assets/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<IndiaAssetDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after updating asset.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting India asset {AssetId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/india-assets/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("India asset {AssetId} deleted", id);
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
                    ? $"India asset {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
