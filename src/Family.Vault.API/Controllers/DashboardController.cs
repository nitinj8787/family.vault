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
    IReadinessScoreService readinessScoreService,
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
        if (userId is null)
            return Unauthorized();

        var insights = await insightService.GetInsightsAsync(userId, cancellationToken);

        logger.LogInformation(
            "Returning {Count} insight(s) for user {UserId}", insights.Count, userId);

        return Ok(insights);
    }

    /// <summary>
    /// Returns the Family Readiness Score for the signed-in user.
    ///
    /// The score (0–100) is a composite of four configurable categories:
    /// <list type="bullet">
    ///   <item>Nominee coverage across all nominee-able assets</item>
    ///   <item>Emergency fund adequacy</item>
    ///   <item>Will availability across recorded jurisdictions</item>
    ///   <item>Documents uploaded to the vault</item>
    /// </list>
    ///
    /// Category weights are controlled by the <c>ReadinessScore</c> configuration section.
    /// The response also includes a per-category breakdown.
    /// </summary>
    [HttpGet("score")]
    [Authorize(Policy = AuthorizationPolicies.FamilyAssetReader)]
    [ProducesResponseType(typeof(ReadinessScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScore(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var score = await readinessScoreService.GetScoreAsync(userId, cancellationToken);

        logger.LogInformation(
            "Returning readiness score {Score}/100 for user {UserId}", score.TotalScore, userId);

        return Ok(score);
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Resolves the user identifier from standard OIDC/Azure AD claims.
    /// Returns <c>null</c> if no recognisable identity claim is present;
    /// callers must respond with 401 in that case.
    /// </summary>
    private string? GetUserId() =>
        User.FindFirst("oid")?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.Identity?.Name;
}

