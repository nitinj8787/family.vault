using Family.Vault.Application.Abstractions;
using Family.Vault.API.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VaultController(
    IFamilyVaultService familyVaultService,
    ILogger<VaultController> logger,
    IOptions<VaultUploadOptions> uploadOptions) : ControllerBase
{
    private readonly VaultUploadOptions _uploadOptions = uploadOptions.Value;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var items = await familyVaultService.GetVaultItemsAsync(cancellationToken);
        logger.LogInformation("Returning {Count} vault items", items.Count);
        return Ok(items);
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        logger.LogInformation("Uploaded vault file {FileName}", file.FileName);
        return Accepted();
    }
}
