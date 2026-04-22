using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/bank-accounts")]
[Authorize]
public sealed class BankAccountsController(
    IBankAccountService bankAccountService,
    ILogger<BankAccountsController> logger) : ControllerBase
{
    /// <summary>Returns all bank accounts belonging to the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var accounts = await bankAccountService.GetAllAsync(userId, cancellationToken);
        logger.LogInformation("Returning {Count} bank accounts for user {UserId}", accounts.Count, userId);
        return Ok(accounts);
    }

    /// <summary>Adds a new bank account for the currently authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] BankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await bankAccountService.AddAsync(userId, request, cancellationToken);
            logger.LogInformation("Bank account {AccountId} created for user {UserId}", response.Id, userId);
            return CreatedAtAction(nameof(GetAll), response);
        }
        catch (BankAccountValidationException ex)
        {
            logger.LogWarning(
                "Bank account validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Updates an existing bank account for the currently authenticated user.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] BankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await bankAccountService.UpdateAsync(userId, id, request, cancellationToken);

            if (response is null)
            {
                logger.LogWarning("Bank account {AccountId} not found for user {UserId}", id, userId);
                return NotFound($"Account '{id}' was not found.");
            }

            logger.LogInformation("Bank account {AccountId} updated for user {UserId}", id, userId);
            return Ok(response);
        }
        catch (BankAccountValidationException ex)
        {
            logger.LogWarning(
                "Bank account validation failed for user {UserId}: {Reason}", userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Deletes a bank account for the currently authenticated user.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var deleted = await bankAccountService.DeleteAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Bank account {AccountId} not found for user {UserId}", id, userId);
            return NotFound($"Account '{id}' was not found.");
        }

        logger.LogInformation("Bank account {AccountId} deleted for user {UserId}", id, userId);
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
