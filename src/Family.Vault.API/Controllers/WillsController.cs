using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/wills")]
[Authorize]
public sealed class WillsController(
    IWillsService willsService,
    ILogger<WillsController> logger) : ControllerBase
{
    /// <summary>Returns all will entries for the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WillEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var entries = await willsService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} will entries for user {UserId}", entries.Count, userId);
        return Ok(entries);
    }

    /// <summary>Adds a new will entry for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WillEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] WillEntryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await willsService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Will entry {EntryId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (WillsValidationException ex)
        {
            logger.LogWarning(
                "Wills validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing will entry for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WillEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] WillEntryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await willsService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Will entry {EntryId} not found for user {UserId}", id, userId);
                return NotFound($"Will entry '{id}' was not found.");
            }

            logger.LogInformation("Will entry {EntryId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (WillsValidationException ex)
        {
            logger.LogWarning(
                "Wills validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a will entry for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await willsService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Will entry {EntryId} not found for user {UserId}", id, userId);
            return NotFound($"Will entry '{id}' was not found.");
        }

        logger.LogInformation("Will entry {EntryId} deleted for user {UserId}", id, userId);
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
