namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>EmergencyFundService</c> when an emergency fund request fails validation rules.
/// </summary>
public sealed class EmergencyFundValidationException : Exception
{
    public EmergencyFundValidationException(string message) : base(message) { }
}
