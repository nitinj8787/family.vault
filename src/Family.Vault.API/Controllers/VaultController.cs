using Family.Vault.Application.Abstractions;
using Family.Vault.API.Authorization;
using Family.Vault.API.Configuration;
using Family.Vault.Application.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VaultController(
    IFamilyVaultService familyVaultService,
    ILogger<VaultController> logger,
    IOptions<VaultUploadOptions> uploadOptions) : ControllerBase
{
    private readonly VaultUploadOptions _uploadOptions = uploadOptions.Value;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FamilyMember)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var items = await familyVaultService.GetVaultItemsAsync(cancellationToken);
        logger.LogInformation("Returning {Count} vault items", items.Count);
        return Ok(items);
    }

    [HttpGet("download/{fileName}")]
    [Authorize(Policy = AuthorizationPolicies.VaultReader)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("File name is required.");
        }

        logger.LogInformation("Download requested for {FileName}", LogSanitizer.Sanitize(fileName));

        var memoryStream = new MemoryStream();

        try
        {
            await familyVaultService.DownloadAsync(fileName, memoryStream, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            await memoryStream.DisposeAsync();
            logger.LogWarning("Download requested for non-existent file {FileName}", LogSanitizer.Sanitize(fileName));
            return NotFound($"File '{fileName}' was not found.");
        }
        catch
        {
            await memoryStream.DisposeAsync();
            throw;
        }

        memoryStream.Position = 0;

        // FileStreamResult takes ownership of the stream and disposes it after the response is written.
        return File(memoryStream, "application/octet-stream", fileName);
    }

    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.FamilyMember)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        if (file.Length > _uploadOptions.MaxUploadBytes)
        {
            return BadRequest($"File exceeds maximum allowed size of {_uploadOptions.MaxUploadBytes} bytes.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!_uploadOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("File type is not allowed.");
        }

        await using var stream = file.OpenReadStream();
        await familyVaultService.UploadAsync(file.FileName, stream, cancellationToken);

        logger.LogInformation("Uploaded vault file {FileName}", LogSanitizer.Sanitize(file.FileName));
        return Accepted();
    }
}
