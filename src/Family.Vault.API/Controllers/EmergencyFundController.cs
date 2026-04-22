using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/emergency-fund")]
[Authorize]
public sealed class EmergencyFundController(
    IEmergencyFundService emergencyFundService,
    ILogger<EmergencyFundController> logger) : ControllerBase
{
    /// <summary>Returns all emergency fund entries for the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmergencyFundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var entries = await emergencyFundService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} emergency fund entries for user {UserId}", entries.Count, userId);
        return Ok(entries);
    }

    /// <summary>Adds a new emergency fund entry for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmergencyFundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] EmergencyFundRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await emergencyFundService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Emergency fund entry {EntryId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (EmergencyFundValidationException ex)
        {
            logger.LogWarning(
                "Emergency fund validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing emergency fund entry for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmergencyFundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] EmergencyFundRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await emergencyFundService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Emergency fund entry {EntryId} not found for user {UserId}", id, userId);
                return NotFound($"Emergency fund entry '{id}' was not found.");
            }

            logger.LogInformation("Emergency fund entry {EntryId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (EmergencyFundValidationException ex)
        {
            logger.LogWarning(
                "Emergency fund validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes an emergency fund entry for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await emergencyFundService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Emergency fund entry {EntryId} not found for user {UserId}", id, userId);
            return NotFound($"Emergency fund entry '{id}' was not found.");
        }

        logger.LogInformation("Emergency fund entry {EntryId} deleted for user {UserId}", id, userId);
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
