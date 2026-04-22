namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>UkAssetService</c> when a UK asset request fails validation rules.
/// </summary>
public sealed class UkAssetValidationException : Exception
{
    public UkAssetValidationException(string message) : base(message) { }
}
