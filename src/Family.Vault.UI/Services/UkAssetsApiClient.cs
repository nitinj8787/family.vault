using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API UK-assets endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class UkAssetsApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<UkAssetsApiClient> logger) : IUkAssetsApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UkAssetDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching UK assets from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/uk-assets");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<UkAssetDisplayModel>>(_jsonOptions, cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} UK assets", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<UkAssetDisplayModel> AddAsync(
        UkAssetFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding UK asset ({Provider})", model.Provider);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/uk-assets")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "add", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<UkAssetDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after adding asset.");
    }

    /// <inheritdoc/>
    public async Task<UkAssetDisplayModel> UpdateAsync(
        UkAssetFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating UK asset {AssetId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/uk-assets/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<UkAssetDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after updating asset.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting UK asset {AssetId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/uk-assets/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("UK asset {AssetId} deleted", id);
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
                    ? $"UK asset {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
