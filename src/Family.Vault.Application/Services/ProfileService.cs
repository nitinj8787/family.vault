using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Domain.Entities;
using Family.Vault.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IProfileService"/>.
/// Profiles are stored per user in a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// and are lost on application restart.
/// Replace this with a persistent store (e.g. a database) for production use.
/// </summary>
public sealed class ProfileService(ILogger<ProfileService> logger) : IProfileService
{
    private readonly ConcurrentDictionary<string, UserProfile> _store =
        new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<ProfileResponse?> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(userId, out var profile);
        return Task.FromResult(profile is null ? null : MapToResponse(profile));
    }

    /// <inheritdoc/>
    public Task<ProfileResponse> SaveProfileAsync(
        string userId,
        ProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var profile = new UserProfile
        {
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address,
            SpouseName = request.SpouseName,
            Children = request.Children
                .Select(c => new ChildDetail(c.Name, c.DateOfBirth))
                .ToList(),
            EmergencyContacts = request.EmergencyContacts
                .Select(e => new EmergencyContact(e.Name, e.Relationship, e.PhoneNumber))
                .ToList()
        };

        _store[userId] = profile;

        logger.LogInformation(
            "Profile saved for user {UserId} ({ChildCount} children, {ContactCount} emergency contacts)",
            userId, profile.Children.Count, profile.EmergencyContacts.Count);

        return Task.FromResult(MapToResponse(profile));
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

    private static ProfileResponse MapToResponse(UserProfile profile) =>
        new(
            FullName: profile.FullName,
            DateOfBirth: profile.DateOfBirth,
            Address: profile.Address,
            SpouseName: profile.SpouseName,
            Children: profile.Children
                .Select(c => new ChildDetailDto(c.Name, c.DateOfBirth))
                .ToList(),
            EmergencyContacts: profile.EmergencyContacts
                .Select(e => new EmergencyContactDto(e.Name, e.Relationship, e.PhoneNumber))
                .ToList());
}
