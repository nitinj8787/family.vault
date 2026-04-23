using System.Text.Json;
using Dapper;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IProfileService"/>.
/// Profile fields are stored in the <c>Profiles</c> table; children are serialised
/// as JSON in the <c>ChildrenJson</c> column; emergency contacts are stored as
/// individual rows in the <c>Contacts</c> table (keyed by UserId).
/// A synthetic user row is upserted into <c>Users</c> so that foreign-key
/// constraints are satisfied without requiring a full authentication layer.
/// </summary>
public sealed class SqliteProfileService(
    FamilyVaultDbContext dbContext,
    ILogger<SqliteProfileService> logger) : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<ProfileResponse?> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
        
        var profile = await conn.QuerySingleOrDefaultAsync<ProfileRow>(
            "SELECT Id, FullName, SpouseName, DOB, Address, ChildrenJson FROM Profiles WHERE UserId = @UserId",
            new { UserId = userId });

        if (profile is null)
            return null;

        var contacts = (await conn.QueryAsync<ContactRow>(
            "SELECT Name, Phone, Relationship FROM Contacts WHERE UserId = @UserId ORDER BY PriorityOrder",
            new { UserId = userId })).ToList();

        return MapToResponse(profile, contacts);
    }

    /// <inheritdoc/>
    public async Task<ProfileResponse> SaveProfileAsync(
        string userId,
        ProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var childrenJson = JsonSerializer.Serialize(
            request.Children.Select(c => new ChildJson(c.Name, c.DateOfBirth?.ToString("yyyy-MM-dd"))),
            JsonOptions);

        var dob = request.DateOfBirth?.ToString("yyyy-MM-dd");

        var conn = await dbContext.OpenConnectionAsync(cancellationToken);
                using var tx = conn.BeginTransaction();

        // Ensure a Users row exists so FK constraints pass.
        await conn.ExecuteAsync(
            """
            INSERT INTO Users (Id, Email, FullName, IsActive)
            VALUES (@Id, @Email, @FullName, 1)
            ON CONFLICT(Id) DO UPDATE SET FullName = @FullName
            """,
            new { Id = userId, Email = $"{userId}@vault.local", FullName = request.FullName },
            tx);

        // Upsert the profile row.
        await conn.ExecuteAsync(
            """
            INSERT INTO Profiles (Id, UserId, FullName, SpouseName, DOB, Address, ChildrenJson, UpdatedAt)
            VALUES (@Id, @UserId, @FullName, @SpouseName, @DOB, @Address, @ChildrenJson, @UpdatedAt)
            ON CONFLICT(UserId) DO UPDATE SET
                FullName     = @FullName,
                SpouseName   = @SpouseName,
                DOB          = @DOB,
                Address      = @Address,
                ChildrenJson = @ChildrenJson,
                UpdatedAt    = @UpdatedAt
            """,
            new
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                request.FullName,
                request.SpouseName,
                DOB = dob,
                request.Address,
                ChildrenJson = childrenJson,
                UpdatedAt = DateTime.UtcNow.ToString("o")
            },
            tx);

        // Replace all emergency contacts.
        await conn.ExecuteAsync(
            "DELETE FROM Contacts WHERE UserId = @UserId",
            new { UserId = userId },
            tx);

        for (var i = 0; i < request.EmergencyContacts.Count; i++)
        {
            var contact = request.EmergencyContacts[i];
            await conn.ExecuteAsync(
                """
                INSERT INTO Contacts (Id, UserId, Name, Phone, Relationship, PriorityOrder)
                VALUES (@Id, @UserId, @Name, @Phone, @Relationship, @PriorityOrder)
                """,
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    contact.Name,
                    Phone = contact.PhoneNumber,
                    contact.Relationship,
                    PriorityOrder = i
                },
                tx);
        }

        tx.Commit();

        logger.LogInformation(
            "Profile saved for user {UserId} ({ChildCount} children, {ContactCount} emergency contacts)",
            userId, request.Children.Count, request.EmergencyContacts.Count);

        return new ProfileResponse(
            FullName: request.FullName,
            DateOfBirth: request.DateOfBirth,
            Address: request.Address,
            SpouseName: request.SpouseName,
            Children: request.Children.ToList(),
            EmergencyContacts: request.EmergencyContacts.ToList());
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static void ValidateRequest(ProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ProfileValidationException("Full name is required.");
        if (request.FullName.Length > 100)
            throw new ProfileValidationException("Full name must not exceed 100 characters.");

        if (request.Address is { Length: > 300 })
            throw new ProfileValidationException("Address must not exceed 300 characters.");

        if (request.SpouseName is { Length: > 100 })
            throw new ProfileValidationException("Spouse name must not exceed 100 characters.");

        foreach (var child in request.Children)
        {
            if (string.IsNullOrWhiteSpace(child.Name))
                throw new ProfileValidationException("Each child must have a name.");
            if (child.Name.Length > 100)
                throw new ProfileValidationException("A child's name must not exceed 100 characters.");
        }

        foreach (var contact in request.EmergencyContacts)
        {
            if (string.IsNullOrWhiteSpace(contact.Name))
                throw new ProfileValidationException("Each emergency contact must have a name.");
            if (string.IsNullOrWhiteSpace(contact.Relationship))
                throw new ProfileValidationException("Each emergency contact must have a relationship.");
            if (string.IsNullOrWhiteSpace(contact.PhoneNumber))
                throw new ProfileValidationException("Each emergency contact must have a phone number.");
        }
    }

    private static ProfileResponse MapToResponse(ProfileRow profile, List<ContactRow> contacts)
    {
        var children = DeserializeChildren(profile.ChildrenJson);

        var emergencyContacts = contacts
            .Select(c => new EmergencyContactDto(c.Name, c.Relationship ?? "", c.Phone ?? ""))
            .ToList();

        DateOnly? dob = null;
        if (!string.IsNullOrWhiteSpace(profile.DOB) &&
            DateOnly.TryParse(profile.DOB, out var parsed))
            dob = parsed;

        return new ProfileResponse(
            FullName: profile.FullName,
            DateOfBirth: dob,
            Address: profile.Address,
            SpouseName: profile.SpouseName,
            Children: children,
            EmergencyContacts: emergencyContacts);
    }

    private static IReadOnlyList<ChildDetailDto> DeserializeChildren(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ChildJson>>(json, JsonOptions) ?? [];
            return items
                .Select(c =>
                {
                    DateOnly? dob = null;
                    if (!string.IsNullOrWhiteSpace(c.Dob) && DateOnly.TryParse(c.Dob, out var parsed))
                        dob = parsed;
                    return new ChildDetailDto(c.Name, dob);
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    // -----------------------------------------------------------------
    // Row / JSON mapping types
    // -----------------------------------------------------------------

    private sealed class ProfileRow
    {
        public string FullName { get; init; } = "";
        public string? SpouseName { get; init; }
        public string? DOB { get; init; }
        public string? Address { get; init; }
        public string? ChildrenJson { get; init; }
    }

    private sealed class ContactRow
    {
        public string Name { get; init; } = "";
        public string? Phone { get; init; }
        public string? Relationship { get; init; }
    }

    private sealed record ChildJson(string Name, string? Dob);
}
