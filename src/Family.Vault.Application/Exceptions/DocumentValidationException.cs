namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>DocumentService</c> when an uploaded document fails validation rules
/// (e.g. file too large, unsupported extension).
/// </summary>
public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message) : base(message) { }
}
