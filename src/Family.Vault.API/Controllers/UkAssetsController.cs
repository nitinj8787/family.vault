using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/uk-assets")]
[Authorize]
public sealed class UkAssetsController(
    IUkAssetService ukAssetService,
    ILogger<UkAssetsController> logger) : ControllerBase
{
    /// <summary>Returns all UK assets belonging to the currently authenticated user.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FamilyAssetReader)]
    [ProducesResponseType(typeof(IReadOnlyList<UkAssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var assets = await ukAssetService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} UK assets for user {UserId}", assets.Count, userId);
        return Ok(assets);
    }

    /// <summary>Adds a new UK asset for the currently authenticated user.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(UkAssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] UkAssetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await ukAssetService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("UK asset {AssetId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (UkAssetValidationException ex)
        {
            logger.LogWarning(
                "UK asset validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing UK asset for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(UkAssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UkAssetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await ukAssetService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("UK asset {AssetId} not found for user {UserId}", id, userId);
                return NotFound($"Asset '{id}' was not found.");
            }

            logger.LogInformation("UK asset {AssetId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (UkAssetValidationException ex)
        {
            logger.LogWarning(
                "UK asset validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a UK asset for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await ukAssetService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("UK asset {AssetId} not found for user {UserId}", id, userId);
            return NotFound($"Asset '{id}' was not found.");
        }

        logger.LogInformation("UK asset {AssetId} deleted for user {UserId}", id, userId);
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
