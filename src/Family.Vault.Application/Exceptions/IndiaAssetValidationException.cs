namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>IndiaAssetService</c> when an India asset request fails validation rules.
/// </summary>
public sealed class IndiaAssetValidationException : Exception
{
    public IndiaAssetValidationException(string message) : base(message) { }
}
