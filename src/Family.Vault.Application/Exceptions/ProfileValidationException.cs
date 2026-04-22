namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>ProfileService</c> when a profile save request fails validation rules
/// (e.g. missing required fields, values exceeding maximum lengths).
/// </summary>
public sealed class ProfileValidationException : Exception
{
    public ProfileValidationException(string message) : base(message) { }
}
