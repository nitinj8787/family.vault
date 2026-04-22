using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/insurance")]
[Authorize]
public sealed class InsuranceController(
    IInsuranceService insuranceService,
    ILogger<InsuranceController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InsuranceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var policies = await insuranceService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} insurance policies for user {UserId}", policies.Count, userId);
        return Ok(policies);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InsuranceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] InsuranceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await insuranceService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Insurance policy {PolicyId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (InsuranceValidationException ex)
        {
            logger.LogWarning(
                "Insurance policy validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InsuranceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] InsuranceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await insuranceService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Insurance policy {PolicyId} not found for user {UserId}", id, userId);
                return NotFound($"Insurance policy '{id}' was not found.");
            }

            logger.LogInformation("Insurance policy {PolicyId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (InsuranceValidationException ex)
        {
            logger.LogWarning(
                "Insurance policy validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await insuranceService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Insurance policy {PolicyId} not found for user {UserId}", id, userId);
            return NotFound($"Insurance policy '{id}' was not found.");
        }

        logger.LogInformation("Insurance policy {PolicyId} deleted for user {UserId}", id, userId);
        return NoContent();
    }

    private string GetUserId() =>
        User.FindFirst("oid")?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.Identity?.Name
        ?? "default";
}
