using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/investments")]
[Authorize]
public sealed class InvestmentsController(
    IInvestmentService investmentService,
    ILogger<InvestmentsController> logger) : ControllerBase
{
    /// <summary>Returns all investments belonging to the currently authenticated user.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FamilyAssetReader)]
    [ProducesResponseType(typeof(IReadOnlyList<InvestmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var investments = await investmentService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} investments for user {UserId}", investments.Count, userId);
        return Ok(investments);
    }

    /// <summary>Adds a new investment for the currently authenticated user.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] InvestmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await investmentService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Investment {InvestmentId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (InvestmentValidationException ex)
        {
            logger.LogWarning(
                "Investment validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing investment for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] InvestmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await investmentService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Investment {InvestmentId} not found for user {UserId}", id, userId);
                return NotFound($"Investment '{id}' was not found.");
            }

            logger.LogInformation("Investment {InvestmentId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (InvestmentValidationException ex)
        {
            logger.LogWarning(
                "Investment validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes an investment for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await investmentService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Investment {InvestmentId} not found for user {UserId}", id, userId);
            return NotFound($"Investment '{id}' was not found.");
        }

        logger.LogInformation("Investment {InvestmentId} deleted for user {UserId}", id, userId);
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
