using Family.Vault.API.Authorization;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DocumentController(
    IDocumentService documentService,
    ILogger<DocumentController> logger) : ControllerBase
{
    /// <summary>
    /// Returns document metadata records for the currently authenticated user.
    /// Optionally filtered by <paramref name="category"/> and/or <paramref name="search"/>.
    /// When neither parameter is supplied all documents are returned, ordered by descending
    /// upload date.
    /// </summary>
    /// <param name="category">
    /// Optional category filter (e.g. <c>Uk</c>, <c>India</c>, <c>Insurance</c>,
    /// <c>Legal</c>, <c>Financial</c>, <c>Medical</c>, <c>Other</c>). Case-insensitive.
    /// </param>
    /// <param name="search">
    /// Optional free-text search term matched against file name and description.
    /// Case-insensitive substring match.
    /// </param>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CriticalDataReader)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentMetadataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        IReadOnlyList<DocumentMetadataResponse> docs;

        if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(search))
        {
            docs = await documentService.GetAllAsync(userId, cancellationToken);
        }
        else
        {
            docs = await documentService.SearchAsync(userId, category, search, cancellationToken);
        }

        logger.LogInformation(
            "Returning {Count} documents for user {UserId} (category={Category}, search={Search})",
            docs.Count, userId,
            LogSanitizer.Sanitize(category),
            LogSanitizer.Sanitize(search));

        return Ok(docs);
    }

    /// <summary>
    /// Uploads a document to family vault storage under the specified category.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="category">
    /// Logical category for the document: <c>Uk</c>, <c>India</c>, <c>Insurance</c>,
    /// <c>Legal</c>, <c>Financial</c>, <c>Medical</c>, or <c>Other</c>.
    /// </param>
    /// <param name="description">Optional free-text description of the document.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] DocumentCategory category,
        [FromQuery] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        await using var stream = file.OpenReadStream();

        var request = new DocumentUploadRequest(
            FileName: file.FileName,
            FileSizeBytes: file.Length,
            Content: stream,
            Category: category,
            Description: description ?? string.Empty);

        DocumentUploadResponse response;

        try
        {
            response = await documentService.UploadAsync(userId, request, cancellationToken);
        }
        catch (DocumentValidationException ex)
        {
            logger.LogWarning(
                "Document upload rejected for {FileName}: {Reason}",
                LogSanitizer.Sanitize(file.FileName),
                ex.Message);

            return BadRequest(ex.Message);
        }

        return CreatedAtAction(nameof(GetAll), response);
    }

    /// <summary>
    /// Downloads a document by its unique identifier for the currently authenticated user.
    /// </summary>
    [HttpGet("download/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CriticalDataReader)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        logger.LogInformation(
            "Download requested for document {DocId} by user {UserId}", id, userId);

        var memoryStream = new MemoryStream();
        string fileName;

        try
        {
            fileName = await documentService.DownloadAsync(userId, id, memoryStream, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            await memoryStream.DisposeAsync();
            logger.LogWarning(
                "Document {DocId} not found for user {UserId}", id, userId);
            return NotFound($"Document '{id}' was not found.");
        }
        catch
        {
            await memoryStream.DisposeAsync();
            throw;
        }

        memoryStream.Position = 0;
        return File(memoryStream, "application/octet-stream", fileName);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Resolves the user identifier from standard OIDC/Azure AD claims.
    /// Returns <c>null</c> if no recognisable identity claim is present;
    /// callers must respond with 401 in that case.
    /// </summary>
    private string? GetUserId() =>
        User.FindFirst("oid")?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.Identity?.Name;
}
