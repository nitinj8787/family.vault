using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/tax")]
[Authorize]
public sealed class TaxController(
    ITaxService taxService,
    ILogger<TaxController> logger) : ControllerBase
{
    /// <summary>Returns all tax-summary entries for the currently authenticated user.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FamilyAssetReader)]
    [ProducesResponseType(typeof(IReadOnlyList<TaxEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var entries = await taxService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} tax entries for user {UserId}", entries.Count, userId);
        return Ok(entries);
    }

    /// <summary>Adds a new tax-summary entry for the currently authenticated user.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(TaxEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] TaxEntryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await taxService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Tax entry {EntryId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (TaxValidationException ex)
        {
            logger.LogWarning(
                "Tax validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing tax-summary entry for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(TaxEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TaxEntryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await taxService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Tax entry {EntryId} not found for user {UserId}", id, userId);
                return NotFound($"Tax entry '{id}' was not found.");
            }

            logger.LogInformation("Tax entry {EntryId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (TaxValidationException ex)
        {
            logger.LogWarning(
                "Tax validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a tax-summary entry for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await taxService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Tax entry {EntryId} not found for user {UserId}", id, userId);
            return NotFound($"Tax entry '{id}' was not found.");
        }

        logger.LogInformation("Tax entry {EntryId} deleted for user {UserId}", id, userId);
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
