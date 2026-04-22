using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize]
public sealed class PropertiesController(
    IPropertyService propertyService,
    ILogger<PropertiesController> logger) : ControllerBase
{
    /// <summary>Returns all properties belonging to the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var properties = await propertyService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} properties for user {UserId}", properties.Count, userId);
        return Ok(properties);
    }

    /// <summary>Adds a new property for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] PropertyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await propertyService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Property {PropertyId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (PropertyValidationException ex)
        {
            logger.LogWarning(
                "Property validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing property for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PropertyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await propertyService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Property {PropertyId} not found for user {UserId}", id, userId);
                return NotFound($"Property '{id}' was not found.");
            }

            logger.LogInformation("Property {PropertyId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (PropertyValidationException ex)
        {
            logger.LogWarning(
                "Property validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a property for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await propertyService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Property {PropertyId} not found for user {UserId}", id, userId);
            return NotFound($"Property '{id}' was not found.");
        }

        logger.LogInformation("Property {PropertyId} deleted for user {UserId}", id, userId);
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
