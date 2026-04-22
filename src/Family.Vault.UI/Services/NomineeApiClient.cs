using System.Net.Http.Headers;
using System.Net.Http.Json;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API nominee endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class NomineeApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<NomineeApiClient> logger) : INomineeApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<NomineeDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching nominee entries from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/nominees");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<NomineeDisplayModel>>(cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} nominee entries", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<NomineeDisplayModel> AddAsync(
        NomineeFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Adding nominee entry (assetType={AssetType}, institution={Institution})",
            model.AssetType, model.Institution);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/nominees")
        {
            Content = JsonContent.Create(new
            {
                model.AssetType,
                model.Institution,
                model.NomineeName,
                model.Relationship
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "create", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<NomineeDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                "Failed to create nominee entry: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task<NomineeDisplayModel> UpdateAsync(
        NomineeFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating nominee entry {NomineeId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/nominees/{model.Id}")
        {
            Content = JsonContent.Create(new
            {
                model.AssetType,
                model.Institution,
                model.NomineeName,
                model.Relationship
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<NomineeDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to update nominee entry {model.Id}: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting nominee entry {NomineeId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/nominees/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Nominee entry {NomineeId} deleted", id);
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
                    ? $"Nominee {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
