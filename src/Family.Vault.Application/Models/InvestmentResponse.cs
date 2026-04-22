using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for an investment.
/// The <see cref="MaskedAccountId"/> field exposes only the last four characters
/// of the account / folio ID so that sensitive data is not leaked to callers.
/// </summary>
/// <param name="Id">Unique investment identifier.</param>
/// <param name="Platform">Name of the investment platform or broker.</param>
/// <param name="Type">Category of investment (Stocks, MutualFund, Pension, Crypto).</param>
/// <param name="MaskedAccountId">Account / folio ID with all but the last four characters replaced by '•'.</param>
/// <param name="Nominee">Name of the nominated beneficiary, if any.</param>
public sealed record InvestmentResponse(
    Guid Id,
    string Platform,
    InvestmentType Type,
    string MaskedAccountId,
    string? Nominee);
