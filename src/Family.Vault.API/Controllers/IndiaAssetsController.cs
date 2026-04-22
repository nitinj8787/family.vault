using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/india-assets")]
[Authorize]
public sealed class IndiaAssetsController(
    IIndiaAssetService indiaAssetService,
    ILogger<IndiaAssetsController> logger) : ControllerBase
{
    /// <summary>Returns all India assets belonging to the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IndiaAssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var assets = await indiaAssetService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} India assets for user {UserId}", assets.Count, userId);
        return Ok(assets);
    }

    /// <summary>Adds a new India asset for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IndiaAssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] IndiaAssetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await indiaAssetService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("India asset {AssetId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (IndiaAssetValidationException ex)
        {
            logger.LogWarning(
                "India asset validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing India asset for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(IndiaAssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] IndiaAssetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await indiaAssetService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("India asset {AssetId} not found for user {UserId}", id, userId);
                return NotFound($"Asset '{id}' was not found.");
            }

            logger.LogInformation("India asset {AssetId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (IndiaAssetValidationException ex)
        {
            logger.LogWarning(
                "India asset validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes an India asset for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await indiaAssetService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("India asset {AssetId} not found for user {UserId}", id, userId);
            return NotFound($"Asset '{id}' was not found.");
        }

        logger.LogInformation("India asset {AssetId} deleted for user {UserId}", id, userId);
        return NoContent();
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private string GetUserId() =>
        User.FindFirst("oid")?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.Identity?.Name
        ?? "default";
}
