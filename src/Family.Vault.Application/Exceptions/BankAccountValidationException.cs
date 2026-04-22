namespace Family.Vault.Application.Exceptions;

/// <summary>
/// Thrown by <c>BankAccountService</c> when a bank account request fails validation rules.
/// </summary>
public sealed class BankAccountValidationException : Exception
{
    public BankAccountValidationException(string message) : base(message) { }
}
