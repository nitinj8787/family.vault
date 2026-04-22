using System.Net.Http.Headers;
using System.Net.Http.Json;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API wills endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class WillsApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<WillsApiClient> logger) : IWillsApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<WillDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching will entries from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/wills");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<WillDisplayModel>>(cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} will entries", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<WillDisplayModel> AddAsync(
        WillFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding will entry (country={Country})", model.Country);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/wills")
        {
            Content = JsonContent.Create(new
            {
                model.Country,
                model.WillExists,
                model.Location,
                model.Executor
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "create", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<WillDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                "Failed to create will entry: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task<WillDisplayModel> UpdateAsync(
        WillFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating will entry {EntryId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/wills/{model.Id}")
        {
            Content = JsonContent.Create(new
            {
                model.Country,
                model.WillExists,
                model.Location,
                model.Executor
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<WillDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to update will entry {model.Id}: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting will entry {EntryId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/wills/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Will entry {EntryId} deleted", id);
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
                    ? $"Wills {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
