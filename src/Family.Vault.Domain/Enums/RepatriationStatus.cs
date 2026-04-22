namespace Family.Vault.Domain.Enums;

/// <summary>
/// Indicates how easily funds in an India asset can be transferred abroad.
/// </summary>
public enum RepatriationStatus
{
    /// <summary>
    /// Funds (principal and income) may be freely remitted abroad.
    /// Typical for NRE accounts and foreign-currency deposits.
    /// </summary>
    FullyRepatriable,

    /// <summary>
    /// Only limited remittances are permitted (e.g. up to USD 1 million
    /// per financial year for NRO accounts, subject to applicable taxes).
    /// </summary>
    Limited
}
