namespace Family.Vault.Domain.Enums;

/// <summary>
/// Classifies an NRI (Non-Resident Indian) bank or investment account type.
/// </summary>
public enum NriAccountType
{
    /// <summary>
    /// Non-Resident External account.
    /// Funded from foreign income; interest is tax-free in India;
    /// principal and interest are fully repatriable.
    /// </summary>
    NRE,

    /// <summary>
    /// Non-Resident Ordinary account.
    /// Holds income earned in India (rent, dividends, etc.);
    /// interest is taxable in India; repatriation is limited
    /// (up to USD 1 million per financial year after applicable taxes).
    /// </summary>
    NRO
}
