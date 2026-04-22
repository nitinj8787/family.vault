using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Family.Vault.UI.Models;

namespace Family.Vault.UI.Services;

/// <summary>
/// HTTP client that calls the FamilyVault API.
/// Bearer token acquisition is delegated to <see cref="ITokenProvider"/>, keeping
/// this class independent of the underlying identity mechanism (Azure AD, placeholder,
/// or any future provider).
/// </summary>
public sealed class VaultApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    ILogger<VaultApiClient> logger) : IVaultApiClient
{
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
        logger.LogInformation("Fetching vault items from API");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/vault");
        await AttachAuthorizationAsync(request, cancellationToken);

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
        await AttachAuthorizationAsync(request, cancellationToken);

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
    /// Acquires a bearer token from <see cref="ITokenProvider"/> and attaches it to
    /// <paramref name="request"/> as a per-request Authorization header.
    /// If the provider returns an empty or null token (e.g. the placeholder in dev
    /// when no static token is configured) no Authorization header is set, so the
    /// request is sent without credentials.
    /// </summary>
    private async Task AttachAuthorizationAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

