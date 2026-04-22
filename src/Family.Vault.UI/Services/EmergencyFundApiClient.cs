using System.Net.Http.Headers;
using System.Net.Http.Json;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API emergency-fund endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class EmergencyFundApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<EmergencyFundApiClient> logger) : IEmergencyFundApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmergencyFundDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching emergency fund entries from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/emergency-fund");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<EmergencyFundDisplayModel>>(cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} emergency fund entries", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<EmergencyFundDisplayModel> AddAsync(
        EmergencyFundFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding emergency fund entry ({Location})", model.Location);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/emergency-fund")
        {
            Content = JsonContent.Create(new
            {
                model.Location,
                model.Amount,
                model.AccessInstructions
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "create", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<EmergencyFundDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                "Failed to create emergency fund entry: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task<EmergencyFundDisplayModel> UpdateAsync(
        EmergencyFundFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating emergency fund entry {EntryId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/emergency-fund/{model.Id}")
        {
            Content = JsonContent.Create(new
            {
                model.Location,
                model.Amount,
                model.AccessInstructions
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<EmergencyFundDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to update emergency fund entry {model.Id}: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting emergency fund entry {EntryId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/emergency-fund/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Emergency fund entry {EntryId} deleted", id);
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
                    ? $"Emergency fund {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
