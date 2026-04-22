namespace Family.Vault.Application.Models;

/// <summary>
/// Response DTO for a wills &amp; legal entry.
/// </summary>
/// <param name="Id">Unique entry identifier.</param>
/// <param name="Country">Jurisdiction / country the will covers.</param>
/// <param name="WillExists">Whether a signed, valid will exists for this jurisdiction.</param>
/// <param name="Location">Physical or digital location of the will.</param>
/// <param name="Executor">Full name of the appointed executor.</param>
public sealed record WillEntryResponse(
    Guid Id,
    string Country,
    bool WillExists,
    string? Location,
    string? Executor);
