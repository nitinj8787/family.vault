using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Models;
using Family.Vault.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Family.Vault.API.Controllers;

/// <summary>
/// Provides aggregated dashboard data for the currently authenticated user.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(
    IInsightService insightService,
    ILogger<DashboardController> logger) : ControllerBase
{
    /// <summary>
    /// Returns a prioritised list of actionable insights for the signed-in user.
    ///
    /// Detects:
    /// <list type="bullet">
    ///   <item>Assets with missing nominees</item>
    ///   <item>No registered will</item>
    ///   <item>Low or missing emergency fund</item>
    ///   <item>Insurance policies expiring within 30 days</item>
    ///   <item>Assets recorded but no documents uploaded</item>
    /// </list>
    ///
    /// Insights are ordered by descending severity (High → Medium → Low).
    /// An empty array is returned when no gaps are detected.
    /// </summary>
    [HttpGet("insights")]
    [Authorize(Policy = AuthorizationPolicies.FamilyAssetReader)]
    [ProducesResponseType(typeof(IReadOnlyList<InsightResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInsights(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var insights = await insightService.GetInsightsAsync(userId, cancellationToken);

        logger.LogInformation(
            "Returning {Count} insight(s) for user {UserId}", insights.Count, userId);

        return Ok(insights);
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
