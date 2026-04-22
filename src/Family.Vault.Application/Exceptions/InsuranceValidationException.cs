namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>InsuranceService</c> when an insurance request fails validation rules.
/// </summary>
public sealed class InsuranceValidationException : Exception
{
    public InsuranceValidationException(string message) : base(message) { }
}
