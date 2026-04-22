namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>InvestmentService</c> when an investment request fails validation rules.
/// </summary>
public sealed class InvestmentValidationException : Exception
{
    public InvestmentValidationException(string message) : base(message) { }
}
