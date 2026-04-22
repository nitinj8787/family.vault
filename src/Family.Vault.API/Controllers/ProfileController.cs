using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController(
    IProfileService profileService,
    ILogger<ProfileController> logger) : ControllerBase
{
    /// <summary>Returns the personal profile for the currently authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await profileService.GetProfileAsync(userId, cancellationToken);

        if (profile is null)
        {
            logger.LogInformation("No profile found for user {UserId}", userId);
            return NoContent();
        }

        return Ok(profile);
    }

    /// <summary>Creates or replaces the personal profile for the currently authenticated user.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Save(
        [FromBody] ProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        try
        {
            var response = await profileService.SaveProfileAsync(userId, request, cancellationToken);
            logger.LogInformation("Profile saved for user {UserId}", userId);
            return Ok(response);
        }
        catch (ProfileValidationException ex)
        {
            logger.LogWarning(
                "Profile validation failed for user {UserId}: {Reason}",
                userId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Extracts the user identifier from the JWT claims.
    /// Falls back to <c>"default"</c> when running without a configured identity provider
    /// (development / placeholder-token mode).
    /// </summary>
    private string GetUserId() =>
        User.FindFirst("oid")?.Value           // Azure AD object ID
        ?? User.FindFirst("sub")?.Value        // Generic JWT subject
        ?? User.Identity?.Name                 // Display name
        ?? "default";
}
