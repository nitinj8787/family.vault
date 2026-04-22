using System.Net.Http.Headers;
using System.Net.Http.Json;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API tax endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class TaxApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<TaxApiClient> logger) : ITaxApiClient
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaxDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching tax entries from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/tax");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<TaxDisplayModel>>(cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} tax entries", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<TaxDisplayModel> AddAsync(
        TaxFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Adding tax entry (incomeType={IncomeType}, country={Country})",
            model.IncomeType, model.Country);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/tax")
        {
            Content = JsonContent.Create(new
            {
                model.IncomeType,
                model.Country,
                model.TaxPaid,
                model.DeclaredInUk
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "create", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<TaxDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                "Failed to create tax entry: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task<TaxDisplayModel> UpdateAsync(
        TaxFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating tax entry {EntryId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/tax/{model.Id}")
        {
            Content = JsonContent.Create(new
            {
                model.IncomeType,
                model.Country,
                model.TaxPaid,
                model.DeclaredInUk
            })
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<TaxDisplayModel>(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to update tax entry {model.Id}: API returned an empty response.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting tax entry {EntryId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/tax/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Tax entry {EntryId} deleted", id);
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
                    ? $"Tax {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
