namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for an emergency fund entry.
/// </summary>
/// <param name="Id">Unique entry identifier.</param>
/// <param name="Location">Name of the bank, account, or location where the fund is held.</param>
/// <param name="Amount">Amount held (in the user's preferred currency).</param>
/// <param name="AccessInstructions">Instructions for accessing the funds in an emergency.</param>
public sealed record EmergencyFundResponse(
    Guid Id,
    string Location,
    decimal Amount,
    string? AccessInstructions);
