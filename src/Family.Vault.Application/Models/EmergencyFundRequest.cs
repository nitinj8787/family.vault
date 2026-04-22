namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating an emergency fund entry.
/// </summary>
/// <param name="Location">Name of the bank, account, or location where the fund is held.</param>
/// <param name="Amount">Amount held (in the user's preferred currency). Must be non-negative.</param>
/// <param name="AccessInstructions">Instructions for accessing the funds in an emergency.</param>
public sealed record EmergencyFundRequest(
    string Location,
    decimal Amount,
    string? AccessInstructions);
