namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>PropertyService</c> when a property request fails validation rules.
/// </summary>
public sealed class PropertyValidationException : Exception
{
    public PropertyValidationException(string message) : base(message) { }
}
