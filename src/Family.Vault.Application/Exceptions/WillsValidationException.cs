namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>WillsService</c> when a will-entry request fails validation rules.
/// </summary>
public sealed class WillsValidationException : Exception
{
    public WillsValidationException(string message) : base(message) { }
}
