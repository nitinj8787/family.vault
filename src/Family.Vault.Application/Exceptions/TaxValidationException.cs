namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>TaxService</c> when a tax-entry request fails validation rules.
/// </summary>
public sealed class TaxValidationException : Exception
{
    public TaxValidationException(string message) : base(message) { }
}
