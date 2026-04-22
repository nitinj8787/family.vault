using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;
using Microsoft.Identity.Web;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API, acquiring a bearer token for the
/// signed-in user on every request via <see cref="ITokenAcquisition"/>.
/// </summary>
public sealed class VaultApiClient(
    HttpClient httpClient,
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration,
    ILogger<VaultApiClient> logger) : IVaultApiClient
{
    private readonly string[] _apiScopes =
        configuration.GetSection("VaultApi:Scopes").Get<string[]>()
        ?? throw new InvalidOperationException("VaultApi:Scopes is not configured.");

    // Shared options: camelCase/PascalCase-insensitive + string enum support.
    // Initialized via new() + object initializer; the Converters list is set once
    // before any instance of this class is used, so initialization is safe.
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VaultFileItem>> GetVaultItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var token = await AcquireTokenAsync();

        logger.LogInformation("Fetching vault items from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/vault");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<VaultFileItem[]>(
            _jsonOptions, cancellationToken);

        logger.LogInformation("Received {Count} vault items", items?.Length ?? 0);
        return items ?? [];
    }

    /// <inheritdoc/>
    public async Task<DocumentUploadResult> UploadDocumentAsync(
        string fileName,
        long fileSizeBytes,
        Stream content,
        string category,
        CancellationToken cancellationToken = default)
    {
        var token = await AcquireTokenAsync();

        logger.LogInformation(
            "Uploading document {FileName} (category={Category}, size={FileSizeBytes} bytes)",
            fileName, category, fileSizeBytes);

        using var formContent = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formContent.Add(streamContent, "file", fileName);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/document/upload?category={Uri.EscapeDataString(category)}")
        {
            Content = formContent
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Upload failed for {FileName} (HTTP {StatusCode}): {Error}",
                fileName, (int)response.StatusCode, error);

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(error) ? $"Upload failed ({(int)response.StatusCode})." : error,
                inner: null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<DocumentUploadResult>(
            _jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API returned an empty response after upload.");

        logger.LogInformation(
            "Document {FileName} uploaded successfully to {StoragePath}",
            fileName, result.StoragePath);

        return result;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Acquires (or retrieves from cache) a bearer token for the signed-in user.
    /// </summary>
    private async Task<string> AcquireTokenAsync() =>
        await tokenAcquisition.GetAccessTokenForUserAsync(_apiScopes);
}
