using Family.Vault.Domain.Enums;

namespace Family.Vault.Domain.Entities;

/// <summary>
/// Represents a single tax-summary entry stored in the Family Vault for a given user.
/// Each entry records a source of income, the country in which it was earned, the tax
/// already paid in that country, and whether it has been declared in the UK.
/// </summary>
public sealed class TaxEntry
{
    public TaxEntry(
        Guid id,
        string userId,
        IncomeType incomeType,
        string country,
        decimal taxPaid,
        bool declaredInUk)
    {
        Id = id;
        UserId = userId;
        IncomeType = incomeType;
        Country = country;
        TaxPaid = taxPaid;
        DeclaredInUk = declaredInUk;
    }

    public Guid Id { get; }
    public string UserId { get; }

    /// <summary>Category of income (e.g. Salary, Rental, Dividend).</summary>
    public IncomeType IncomeType { get; }

    /// <summary>Country in which the income was earned or the tax was paid.</summary>
    public string Country { get; }

    /// <summary>Amount of tax already paid in <see cref="Country"/> (in the user's preferred currency).</summary>
    public decimal TaxPaid { get; }

    /// <summary>
    /// Whether this income has been declared on a UK Self Assessment (or equivalent) return.
    /// A value of <c>false</c> for foreign income flags a potential undeclared-income issue.
    /// </summary>
    public bool DeclaredInUk { get; }
}
