using Family.Vault.Domain.Enums;

namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a tax-summary entry.
/// </summary>
/// <param name="Id">Unique entry identifier.</param>
/// <param name="IncomeType">Category of income.</param>
/// <param name="Country">Country where the income was earned / tax was paid.</param>
/// <param name="TaxPaid">Amount of tax already paid in <paramref name="Country"/>.</param>
/// <param name="DeclaredInUk">Whether the income has been declared on a UK return.</param>
public sealed record TaxEntryResponse(
    Guid Id,
    IncomeType IncomeType,
    string Country,
    decimal TaxPaid,
    bool DeclaredInUk);
