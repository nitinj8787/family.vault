using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API bank-accounts endpoints.
/// Authentication is delegated to <see cref="ITokenProvider"/>.
/// </summary>
public sealed class BankAccountsApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<BankAccountsApiClient> logger) : IBankAccountsApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BankAccountDisplayModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching bank accounts from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/bank-accounts");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<BankAccountDisplayModel>>(_jsonOptions, cancellationToken)
            ?? [];

        logger.LogInformation("Loaded {Count} bank accounts", items.Count);
        return items;
    }

    /// <inheritdoc/>
    public async Task<BankAccountDisplayModel> AddAsync(
        BankAccountFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding bank account ({BankName})", model.BankName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/bank-accounts")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "add", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<BankAccountDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after adding bank account.");
    }

    /// <inheritdoc/>
    public async Task<BankAccountDisplayModel> UpdateAsync(
        BankAccountFormModel model,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating bank account {AccountId}", model.Id);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/bank-accounts/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "update", cancellationToken);

        return await response.Content
            .ReadFromJsonAsync<BankAccountDisplayModel>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after updating bank account.");
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting bank account {AccountId}", id);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/bank-accounts/{id}");
        await AttachAuthorizationAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfFailureAsync(response, "delete", cancellationToken);

        logger.LogInformation("Bank account {AccountId} deleted", id);
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
                    ? $"Bank account {operation} failed ({(int)response.StatusCode})."
                    : error,
                inner: null,
                response.StatusCode);
        }
    }
}
