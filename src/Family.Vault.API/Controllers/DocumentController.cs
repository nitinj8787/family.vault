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
    /// Uploads a document to family vault storage under the specified category.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="category">
    /// Logical category for the document: <c>Uk</c>, <c>India</c>, or <c>Insurance</c>.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.FamilyMember)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] DocumentCategory category,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var stream = file.OpenReadStream();

        var request = new DocumentUploadRequest(
            FileName: file.FileName,
            FileSizeBytes: file.Length,
            Content: stream,
            Category: category);

        DocumentUploadResponse response;

        try
        {
            response = await documentService.UploadAsync(request, cancellationToken);
        }
        catch (DocumentValidationException ex)
        {
            logger.LogWarning(
                "Document upload rejected for {FileName}: {Reason}",
                LogSanitizer.Sanitize(file.FileName),
                ex.Message);

            return BadRequest(ex.Message);
        }

        return CreatedAtAction(nameof(Upload), response);
    }
}
