namespace Family.Vault.Application.Models;

/// <summary>
/// Request payload for creating or updating a wills &amp; legal entry.
/// </summary>
/// <param name="Country">Jurisdiction / country the will covers.</param>
/// <param name="WillExists">Whether a signed, valid will exists for this jurisdiction.</param>
/// <param name="Location">Physical or digital location of the will (optional).</param>
/// <param name="Executor">Full name of the appointed executor (optional).</param>
public sealed record WillEntryRequest(
    string Country,
    bool WillExists,
    string? Location,
    string? Executor);
