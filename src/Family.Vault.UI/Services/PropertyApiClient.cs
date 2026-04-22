using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API properties endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class PropertyApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<PropertyApiClient> logger) : IPropertyApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PropertyDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching properties from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/properties");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<PropertyDisplayModel>>(_jsonOptions, cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} properties", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<PropertyDisplayModel> AddAsync(
        PropertyFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding property ({AssetName})", model.AssetName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/properties")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "create", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<PropertyDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to create property: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task<PropertyDisplayModel> UpdateAsync(
        PropertyFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating property {PropertyId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/properties/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<PropertyDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to update property {model.Id}: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting property {PropertyId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/properties/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Property {PropertyId} deleted", id);
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
                    ? $"Property {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
