namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>NomineeService</c> when a nominee request fails validation rules.
/// </summary>
public sealed class NomineeValidationException : Exception
{
    public NomineeValidationException(string message) : base(message) { }
}
