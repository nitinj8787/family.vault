using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Configuration;
using Family.Vault.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Family.Vault.Application.Services;

/// <summary>
/// Calculates the Family Readiness Score by analysing a user's vault data through the
/// existing domain-service abstractions.  All data is loaded in parallel and the
/// category weights are driven entirely by <see cref="ReadinessScoreOptions"/>, making
/// the algorithm easy to tune without redeployment.
/// </summary>
public sealed class ReadinessScoreService(
    IUkAssetService      ukAssetService,
    IIndiaAssetService   indiaAssetService,
    IBankAccountService  bankAccountService,
    IInvestmentService   investmentService,
    IInsuranceService    insuranceService,
    IEmergencyFundService emergencyFundService,
    IWillsService        willsService,
    IDocumentService     documentService,
    IOptions<ReadinessScoreOptions> scoreOptions,
    ILogger<ReadinessScoreService> logger) : IReadinessScoreService
{
    private readonly ReadinessScoreOptions _options = scoreOptions.Value;

    /// <inheritdoc/>
    public async Task<ReadinessScoreResponse> GetScoreAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Fan-out all data reads in parallel.
        var t1 = ukAssetService.GetAllAsync(userId, cancellationToken);
        var t2 = indiaAssetService.GetAllAsync(userId, cancellationToken);
        var t3 = bankAccountService.GetAllAsync(userId, cancellationToken);
        var t4 = investmentService.GetAllAsync(userId, cancellationToken);
        var t5 = insuranceService.GetAllAsync(userId, cancellationToken);
        var t6 = emergencyFundService.GetAllAsync(userId, cancellationToken);
        var t7 = willsService.GetAllAsync(userId, cancellationToken);
        var t8 = documentService.GetAllAsync(userId, cancellationToken);

        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);

        var ukAssets      = await t1;
        var indiaAssets   = await t2;
        var bankAccounts  = await t3;
        var investments   = await t4;
        var insurance     = await t5;
        var emergencyFund = await t6;
        var wills         = await t7;
        var documents     = await t8;

        // Normalised weights so the total always scales to 100.
        double totalRawWeight =
            _options.NomineesWeight
            + _options.EmergencyFundWeight
            + _options.WillsWeight
            + _options.DocumentsWeight;

        // Guard against a misconfigured all-zero weight set.
        if (totalRawWeight <= 0)
        {
            logger.LogWarning(
                "ReadinessScoreOptions has all-zero weights (NomineesWeight={N}, EmergencyFundWeight={E}, " +
                "WillsWeight={W}, DocumentsWeight={D}). Falling back to equal 25/25/25/25 distribution. " +
                "Please review the ReadinessScore configuration section.",
                _options.NomineesWeight, _options.EmergencyFundWeight,
                _options.WillsWeight, _options.DocumentsWeight);
            totalRawWeight = 100;
        }

        double scale = 100.0 / totalRawWeight;

        int nomineesMax      = (int)Math.Round(_options.NomineesWeight      * scale);
        int emergencyFundMax = (int)Math.Round(_options.EmergencyFundWeight * scale);
        int willsMax         = (int)Math.Round(_options.WillsWeight         * scale);
        // Absorb rounding remainder; clamp to non-negative.
        int documentsMax     = Math.Max(0, 100 - nomineesMax - emergencyFundMax - willsMax);

        var nomineesCategory      = ScoreNominees(ukAssets, indiaAssets, bankAccounts, investments, insurance, nomineesMax);
        var emergencyFundCategory = ScoreEmergencyFund(emergencyFund, emergencyFundMax);
        var willsCategory         = ScoreWills(wills, willsMax);
        var documentsCategory     = ScoreDocuments(documents, ukAssets, indiaAssets, bankAccounts, investments, insurance, documentsMax);

        var categories = new List<ScoreCategoryResult>
        {
            nomineesCategory,
            emergencyFundCategory,
            willsCategory,
            documentsCategory
        };

        int totalScore = categories.Sum(c => c.Score);

        logger.LogInformation(
            "Readiness score for user {UserId}: {Total}/100 " +
            "(nominees={N}/{NMax}, emergencyFund={E}/{EMax}, wills={W}/{WMax}, documents={D}/{DMax})",
            userId, totalScore,
            nomineesCategory.Score,      nomineesMax,
            emergencyFundCategory.Score, emergencyFundMax,
            willsCategory.Score,         willsMax,
            documentsCategory.Score,     documentsMax);

        return new ReadinessScoreResponse(
            TotalScore: totalScore,
            MaxScore: 100,
            Categories: categories);
    }

    // -----------------------------------------------------------------
    // Category scorers
    // -----------------------------------------------------------------

    private static ScoreCategoryResult ScoreNominees(
        IReadOnlyList<UkAssetResponse>     ukAssets,
        IReadOnlyList<IndiaAssetResponse>  indiaAssets,
        IReadOnlyList<BankAccountResponse> bankAccounts,
        IReadOnlyList<InvestmentResponse>  investments,
        IReadOnlyList<InsuranceResponse>   insurance,
        int maxScore)
    {
        int nomineeableAssets =
            ukAssets.Count + indiaAssets.Count + bankAccounts.Count
            + investments.Count + insurance.Count;

        if (nomineeableAssets == 0)
        {
            return new ScoreCategoryResult(
                CategoryName: "Nominees",
                Score: 0,
                MaxScore: maxScore,
                Description: "No nominee-able assets recorded yet.");
        }

        int withoutNominee =
            ukAssets.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + indiaAssets.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + bankAccounts.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + investments.Count(a => string.IsNullOrWhiteSpace(a.Nominee))
            + insurance.Count(a => string.IsNullOrWhiteSpace(a.Nominee));

        int covered = nomineeableAssets - withoutNominee;
        int score   = (int)Math.Round(maxScore * (double)covered / nomineeableAssets);

        string description = withoutNominee == 0
            ? $"All {nomineeableAssets} assets have a nominee."
            : $"{covered} of {nomineeableAssets} assets have a nominee; {withoutNominee} still missing.";

        return new ScoreCategoryResult(
            CategoryName: "Nominees",
            Score: score,
            MaxScore: maxScore,
            Description: description);
    }

    private ScoreCategoryResult ScoreEmergencyFund(
        IReadOnlyList<EmergencyFundResponse> emergencyFund,
        int maxScore)
    {
        if (emergencyFund.Count == 0)
        {
            return new ScoreCategoryResult(
                CategoryName: "Emergency Fund",
                Score: 0,
                MaxScore: maxScore,
                Description: "No emergency fund recorded.");
        }

        decimal total = emergencyFund.Sum(e => e.Amount);

        if (total <= 0m)
        {
            return new ScoreCategoryResult(
                CategoryName: "Emergency Fund",
                Score: 0,
                MaxScore: maxScore,
                Description: "Emergency fund total is zero.");
        }

        // Score scales linearly from 0 up to maxScore at the recommended minimum, capped there.
        double ratio = Math.Min((double)total / (double)_options.MinimumRecommendedEmergencyFund, 1.0);
        int score    = (int)Math.Round(maxScore * ratio);

        string description = ratio >= 1.0
            ? $"Emergency fund meets the recommended minimum ({_options.MinimumRecommendedEmergencyFund:N0})."
            : $"Emergency fund ({total:N0}) is below the recommended minimum ({_options.MinimumRecommendedEmergencyFund:N0}).";

        return new ScoreCategoryResult(
            CategoryName: "Emergency Fund",
            Score: score,
            MaxScore: maxScore,
            Description: description);
    }

    private static ScoreCategoryResult ScoreWills(
        IReadOnlyList<WillEntryResponse> wills,
        int maxScore)
    {
        if (wills.Count == 0)
        {
            return new ScoreCategoryResult(
                CategoryName: "Wills & Legal",
                Score: 0,
                MaxScore: maxScore,
                Description: "No will registered for any jurisdiction.");
        }

        int withWill = wills.Count(w => w.WillExists);
        int score    = (int)Math.Round(maxScore * (double)withWill / wills.Count);

        string description = withWill == wills.Count
            ? $"Will on record for all {wills.Count} jurisdiction{(wills.Count == 1 ? "" : "s")}."
            : $"Will on record for {withWill} of {wills.Count} jurisdiction{(wills.Count == 1 ? "" : "s")}.";

        return new ScoreCategoryResult(
            CategoryName: "Wills & Legal",
            Score: score,
            MaxScore: maxScore,
            Description: description);
    }

    private static ScoreCategoryResult ScoreDocuments(
        IReadOnlyList<DocumentMetadataResponse> documents,
        IReadOnlyList<UkAssetResponse>          ukAssets,
        IReadOnlyList<IndiaAssetResponse>       indiaAssets,
        IReadOnlyList<BankAccountResponse>      bankAccounts,
        IReadOnlyList<InvestmentResponse>       investments,
        IReadOnlyList<InsuranceResponse>        insurance,
        int maxScore)
    {
        int totalAssets = ukAssets.Count + indiaAssets.Count + bankAccounts.Count
                          + investments.Count + insurance.Count;

        if (totalAssets == 0)
        {
            return new ScoreCategoryResult(
                CategoryName: "Documents",
                Score: 0,
                MaxScore: maxScore,
                Description: "No assets recorded yet; add assets to track document coverage.");
        }

        if (documents.Count == 0)
        {
            return new ScoreCategoryResult(
                CategoryName: "Documents",
                Score: 0,
                MaxScore: maxScore,
                Description: "No documents have been uploaded to the vault.");
        }

        // Full score when at least one document per asset exists; partial score otherwise.
        double ratio = Math.Min((double)documents.Count / totalAssets, 1.0);
        int score    = (int)Math.Round(maxScore * ratio);

        string description = documents.Count >= totalAssets
            ? $"{documents.Count} document{(documents.Count == 1 ? "" : "s")} uploaded — vault is well documented."
            : $"{documents.Count} document{(documents.Count == 1 ? "" : "s")} uploaded for {totalAssets} assets.";

        return new ScoreCategoryResult(
            CategoryName: "Documents",
            Score: score,
            MaxScore: maxScore,
            Description: description);
    }
}
