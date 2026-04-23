using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Models;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// Analyses a user's vault data and returns a prioritised list of actionable insights.
/// All data is loaded in parallel via the existing domain-service abstractions; no
/// infrastructure dependencies or UI logic are introduced here.
/// </summary>
public sealed class InsightService(
    IWillsService willsService,
    IBankAccountService bankAccountService,
    IUkAssetService ukAssetService,
    IIndiaAssetService indiaAssetService,
    IInvestmentService investmentService,
    IInsuranceService insuranceService,
    IEmergencyFundService emergencyFundService,
    IDocumentService documentService,
    ILogger<InsightService> logger) : IInsightService
{
    /// <summary>
    /// Policies expiring within this many days trigger an insight.
    /// </summary>
    private const int ExpiringInsuranceWarningDays = 30;

    /// <summary>
    /// Policies expiring within this many days are elevated to <see cref="InsightSeverity.High"/>.
    /// </summary>
    private const int ExpiringInsuranceCriticalDays = 7;

    /// <summary>
    /// Emergency-fund total below this amount (in the user's stored currency)
    /// is flagged as a medium-severity gap.
    /// </summary>
    private const decimal MinimumRecommendedEmergencyFund = 1_000m;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InsightResponse>> GetInsightsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Fan-out all data reads in parallel — no sequential blocking.
        var t1 = willsService.GetAllAsync(userId, cancellationToken);
        var t2 = bankAccountService.GetAllAsync(userId, cancellationToken);
        var t3 = ukAssetService.GetAllAsync(userId, cancellationToken);
        var t4 = indiaAssetService.GetAllAsync(userId, cancellationToken);
        var t5 = investmentService.GetAllAsync(userId, cancellationToken);
        var t6 = insuranceService.GetAllAsync(userId, cancellationToken);
        var t7 = emergencyFundService.GetAllAsync(userId, cancellationToken);
        var t8 = documentService.GetAllAsync(userId, cancellationToken);

        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);

        var wills         = await t1;
        var bankAccounts  = await t2;
        var ukAssets      = await t3;
        var indiaAssets   = await t4;
        var investments   = await t5;
        var insurance     = await t6;
        var emergencyFund = await t7;
        var documents     = await t8;

        var insights = new List<InsightResponse>();

        DetectMissingNominees(bankAccounts, ukAssets, indiaAssets, investments, insurance, insights);
        DetectNoWill(wills, insights);
        DetectLowEmergencyFund(emergencyFund, insights);
        DetectExpiringInsurance(insurance, insights);
        DetectAssetsWithoutDocuments(ukAssets, indiaAssets, investments, insurance, bankAccounts, documents, insights);

        // Sort: High → Medium → Low so the most urgent items appear first.
        insights.Sort((a, b) => b.Severity.CompareTo(a.Severity));

        logger.LogInformation(
            "Generated {Count} insight(s) for user {UserId} " +
            "(high={High}, medium={Medium}, low={Low})",
            insights.Count, userId,
            insights.Count(i => i.Severity == InsightSeverity.High),
            insights.Count(i => i.Severity == InsightSeverity.Medium),
            insights.Count(i => i.Severity == InsightSeverity.Low));

        return insights;
    }

    // -----------------------------------------------------------------
    // Detection helpers
    // -----------------------------------------------------------------

    private static void DetectMissingNominees(
        IReadOnlyList<BankAccountResponse>  bankAccounts,
        IReadOnlyList<UkAssetResponse>      ukAssets,
        IReadOnlyList<IndiaAssetResponse>   indiaAssets,
        IReadOnlyList<InvestmentResponse>   investments,
        IReadOnlyList<InsuranceResponse>    insurance,
        List<InsightResponse>               insights)
    {
        int count =
            bankAccounts.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + ukAssets.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + indiaAssets.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + investments.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + insurance.Count(a => string.IsNullOrWhiteSpace(a.Nominee));

        if (count <= 0)
            return;

        var severity = count >= 5 ? InsightSeverity.High : InsightSeverity.Medium;
        insights.Add(new InsightResponse(
            Title: $"{count} asset{(count == 1 ? "" : "s")} {(count == 1 ? "has" : "have")} no nominee",
            Severity: severity,
            SuggestedAction:
                "Open each asset (bank accounts, insurance, investments, etc.) and assign a nominated beneficiary."));
    }

    private static void DetectNoWill(
        IReadOnlyList<WillEntryResponse> wills,
        List<InsightResponse> insights)
    {
        if (wills.Count == 0)
        {
            insights.Add(new InsightResponse(
                Title: "No will has been registered",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    "Record your will details in the Wills & Legal section to protect your family's future."));
            return;
        }

        int missing = wills.Count(w => !w.WillExists);
        if (missing == wills.Count)
        {
            insights.Add(new InsightResponse(
                Title: "No valid will found for any recorded jurisdiction",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    "Update at least one will entry with WillExists = true in the Wills & Legal section."));
        }
        else if (missing > 0)
        {
            insights.Add(new InsightResponse(
                Title: $"Will missing for {missing} jurisdiction{(missing == 1 ? "" : "s")}",
                Severity: InsightSeverity.Medium,
                SuggestedAction:
                    "Open the Wills & Legal section and ensure a will is recorded for every relevant country."));
        }
    }

    private static void DetectLowEmergencyFund(
        IReadOnlyList<EmergencyFundResponse> emergencyFund,
        List<InsightResponse> insights)
    {
        if (emergencyFund.Count == 0)
        {
            insights.Add(new InsightResponse(
                Title: "No emergency fund recorded",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    "Add at least one emergency fund entry so your family knows where to access money in a crisis."));
            return;
        }

        var total = emergencyFund.Sum(e => e.Amount);

        if (total == 0m)
        {
            insights.Add(new InsightResponse(
                Title: "Emergency fund total is zero",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    "Update your emergency fund entries to reflect the actual amount available."));
        }
        else if (total < MinimumRecommendedEmergencyFund)
        {
            insights.Add(new InsightResponse(
                Title: $"Emergency fund is below the recommended minimum ({MinimumRecommendedEmergencyFund:N0})",
                Severity: InsightSeverity.Medium,
                SuggestedAction:
                    "Consider building up your emergency fund to cover at least 3–6 months of essential expenses."));
        }
    }

    private static void DetectExpiringInsurance(
        IReadOnlyList<InsuranceResponse> insurance,
        List<InsightResponse> insights)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warningCutoff  = today.AddDays(ExpiringInsuranceWarningDays);
        var criticalCutoff = today.AddDays(ExpiringInsuranceCriticalDays);

        var expired  = insurance.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < today).ToList();
        var critical = insurance.Where(p => p.ExpiryDate.HasValue
                                            && p.ExpiryDate.Value >= today
                                            && p.ExpiryDate.Value <= criticalCutoff).ToList();
        var warning  = insurance.Where(p => p.ExpiryDate.HasValue
                                            && p.ExpiryDate.Value > criticalCutoff
                                            && p.ExpiryDate.Value <= warningCutoff).ToList();

        if (expired.Count > 0)
        {
            var names = string.Join(", ", expired.Select(p => p.Provider));
            insights.Add(new InsightResponse(
                Title: $"{expired.Count} insurance {(expired.Count == 1 ? "policy has" : "policies have")} already expired",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    $"Renew or replace the following expired {(expired.Count == 1 ? "policy" : "policies")}: {names}."));
        }

        if (critical.Count > 0)
        {
            var names = string.Join(", ", critical.Select(p => p.Provider));
            insights.Add(new InsightResponse(
                Title: $"{critical.Count} insurance {(critical.Count == 1 ? "policy expires" : "policies expire")} within 7 days",
                Severity: InsightSeverity.High,
                SuggestedAction:
                    $"Renew or replace urgently: {names}."));
        }

        if (warning.Count > 0)
        {
            var names = string.Join(", ", warning.Select(p => p.Provider));
            insights.Add(new InsightResponse(
                Title: $"{warning.Count} insurance {(warning.Count == 1 ? "policy expires" : "policies expire")} within 30 days",
                Severity: InsightSeverity.Medium,
                SuggestedAction:
                    $"Schedule renewal for: {names}."));
        }
    }

    private static void DetectAssetsWithoutDocuments(
        IReadOnlyList<UkAssetResponse>     ukAssets,
        IReadOnlyList<IndiaAssetResponse>  indiaAssets,
        IReadOnlyList<InvestmentResponse>  investments,
        IReadOnlyList<InsuranceResponse>   insurance,
        IReadOnlyList<BankAccountResponse> bankAccounts,
        IReadOnlyList<DocumentMetadataResponse> documents,
        List<InsightResponse> insights)
    {
        int totalAssets = ukAssets.Count + indiaAssets.Count + investments.Count
                          + insurance.Count + bankAccounts.Count;

        if (totalAssets > 0 && documents.Count == 0)
        {
            insights.Add(new InsightResponse(
                Title: "No documents uploaded to the vault",
                Severity: InsightSeverity.Medium,
                SuggestedAction:
                    "Upload identity, legal, and financial documents so they are accessible in an emergency."));
        }
    }
}
