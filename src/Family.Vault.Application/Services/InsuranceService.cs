using System.Collections.Concurrent;
using Family.Vault.Application.Abstractions;
using Family.Vault.Application.Exceptions;
using Family.Vault.Application.Models;
using Family.Vault.Application.Utilities;
using Family.Vault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Family.Vault.Application.Services;

/// <summary>
/// In-memory implementation of <see cref="IInsuranceService"/>.
/// Policies are keyed by <c>(userId, policyId)</c> and are lost on application restart.
/// Replace this with a persistent, DB-backed store for production use.
/// </summary>
public sealed class InsuranceService(ILogger<InsuranceService> logger) : IInsuranceService
{
    private readonly ConcurrentDictionary<(string UserId, Guid PolicyId), InsurancePolicy> _policyStore =
        new();

    public Task<IReadOnlyList<InsuranceResponse>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var policies = _policyStore.Values
            .Where(p => p.UserId == userId)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult<IReadOnlyList<InsuranceResponse>>(policies);
    }

    public Task<InsuranceResponse> AddAsync(
        string userId,
        InsuranceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var id = Guid.NewGuid();
        var policy = new InsurancePolicy(
            id, userId, request.Provider, request.PolicyType, request.PolicyNumber,
            request.Coverage, request.Nominee, request.ClaimContact);

        _policyStore[(userId, id)] = policy;

        logger.LogInformation(
            "Insurance policy {PolicyId} added for user {UserId} (provider={SanitizedProvider}, policyType={SanitizedPolicyType})",
            id, userId, LogSanitizer.Sanitize(request.Provider), LogSanitizer.Sanitize(request.PolicyType));

        return Task.FromResult(MapToResponse(policy));
    }

    public Task<InsuranceResponse?> UpdateAsync(
        string userId,
        Guid id,
        InsuranceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        if (!_policyStore.ContainsKey((userId, id)))
        {
            logger.LogWarning(
                "Update requested for non-existent insurance policy {PolicyId} for user {UserId}", id, userId);
            return Task.FromResult<InsuranceResponse?>(null);
        }

        var updated = new InsurancePolicy(
            id, userId, request.Provider, request.PolicyType, request.PolicyNumber,
            request.Coverage, request.Nominee, request.ClaimContact);

        _policyStore[(userId, id)] = updated;

        logger.LogInformation("Insurance policy {PolicyId} updated for user {UserId}", id, userId);
        return Task.FromResult<InsuranceResponse?>(MapToResponse(updated));
    }

    public Task<bool> DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var removed = _policyStore.TryRemove((userId, id), out _);

        if (removed)
            logger.LogInformation("Insurance policy {PolicyId} deleted for user {UserId}", id, userId);
        else
            logger.LogWarning("Delete requested for non-existent insurance policy {PolicyId} for user {UserId}", id, userId);

        return Task.FromResult(removed);
    }

    private static void ValidateRequest(InsuranceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new InsuranceValidationException("Provider is required.");
        if (request.Provider.Length > 150)
            throw new InsuranceValidationException("Provider must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(request.PolicyType))
            throw new InsuranceValidationException("Policy type is required.");
        if (request.PolicyType.Length > 100)
            throw new InsuranceValidationException("Policy type must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.PolicyNumber))
            throw new InsuranceValidationException("Policy number is required.");
        if (request.PolicyNumber.Length > 60)
            throw new InsuranceValidationException("Policy number must not exceed 60 characters.");

        if (string.IsNullOrWhiteSpace(request.Coverage))
            throw new InsuranceValidationException("Coverage is required.");
        if (request.Coverage.Length > 120)
            throw new InsuranceValidationException("Coverage must not exceed 120 characters.");

        if (request.Nominee is { Length: > 100 })
            throw new InsuranceValidationException("Nominee name must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(request.ClaimContact))
            throw new InsuranceValidationException("Claim contact is required.");
        if (request.ClaimContact.Length > 120)
            throw new InsuranceValidationException("Claim contact must not exceed 120 characters.");
    }

    private static InsuranceResponse MapToResponse(InsurancePolicy policy) =>
        new(
            Id: policy.Id,
            Provider: policy.Provider,
            PolicyType: policy.PolicyType,
            PolicyNumber: policy.PolicyNumber,
            Coverage: policy.Coverage,
            Nominee: policy.Nominee,
            ClaimContact: policy.ClaimContact);
}
