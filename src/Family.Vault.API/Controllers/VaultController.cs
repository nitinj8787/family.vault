using Family.Vault.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VaultController(IFamilyVaultService familyVaultService, ILogger<VaultController> logger) : ControllerBase
{
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
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var stream = file.OpenReadStream();
        await familyVaultService.UploadAsync(file.FileName, stream, cancellationToken);

        logger.LogInformation("Uploaded vault file {FileName}", file.FileName);
        return Accepted();
    }
}
