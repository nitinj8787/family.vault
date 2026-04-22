using System.Net.Http.Headers;
using System.Net.Http.Json;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API insurance endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class InsuranceApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<InsuranceApiClient> logger) : IInsuranceApiClient
{
    public async Task<IReadOnlyList<InsuranceDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching insurance policies from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/insurance");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<InsuranceDisplayModel>>(cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} insurance policies", items.Count);
        return items;
    }

    public async Task<InsuranceDisplayModel> AddAsync(
        InsuranceFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding insurance policy ({Provider})", model.Provider);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/insurance")
        {
            Content = JsonContent.Create(model)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "add", cancellationToken);

        return await response.Content.ReadFromJsonAsync<InsuranceDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after adding insurance policy.");
    }

    public async Task<InsuranceDisplayModel> UpdateAsync(
        InsuranceFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating insurance policy {PolicyId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/insurance/{model.Id}")
        {
            Content = JsonContent.Create(model)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content.ReadFromJsonAsync<InsuranceDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after updating insurance policy.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting insurance policy {PolicyId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/insurance/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Insurance policy {PolicyId} deleted", id);
    }

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
                    ? $"Insurance policy {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
