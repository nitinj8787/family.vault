using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/nominees")]
[Authorize]
public sealed class NomineeController(
    INomineeService nomineeService,
    ILogger<NomineeController> logger) : ControllerBase
{
    /// <summary>Returns all nominee entries for the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NomineeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var entries = await nomineeService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} nominee entries for user {UserId}", entries.Count, userId);
        return Ok(entries);
    }

    /// <summary>Adds a new nominee entry for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(NomineeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] NomineeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await nomineeService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Nominee entry {NomineeId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (NomineeValidationException ex)
        {
            logger.LogWarning(
                "Nominee validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing nominee entry for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NomineeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] NomineeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await nomineeService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Nominee entry {NomineeId} not found for user {UserId}", id, userId);
                return NotFound($"Nominee entry '{id}' was not found.");
            }

            logger.LogInformation("Nominee entry {NomineeId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (NomineeValidationException ex)
        {
            logger.LogWarning(
                "Nominee validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a nominee entry for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await nomineeService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Nominee entry {NomineeId} not found for user {UserId}", id, userId);
            return NotFound($"Nominee entry '{id}' was not found.");
        }

        logger.LogInformation("Nominee entry {NomineeId} deleted for user {UserId}", id, userId);
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
