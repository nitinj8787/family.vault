namespace Family.Vault.Infrastructure.Database.Entities;

public sealed class UserEntity
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? FullName { get; set; }
    public string? CreatedAt { get; set; }
    public string? LastLoginAt { get; set; }
    public int IsActive { get; set; } = 1;
}

public sealed class ProfileEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? SpouseName { get; set; }
    public string? DOB { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string ChildrenJson { get; set; } = "[]";
    public string? UpdatedAt { get; set; }
}

public sealed class ContactEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Relationship { get; set; }
    public int? PriorityOrder { get; set; }
}

public sealed class AssetEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Provider { get; set; }
    public string? NomineeId { get; set; }
    public int IsActive { get; set; } = 1;
    public string? CreatedAt { get; set; }
}

public sealed class BankAccountEntity
{
    public string Id { get; set; } = "";
    public string? AccountNumber { get; set; }
    public string? AccountType { get; set; }
    public string? IFSC_SWIFT { get; set; }
    public int IsPrimary { get; set; }
    public string? Nominee { get; set; }
}

public sealed class InvestmentEntity
{
    public string Id { get; set; } = "";
    public string? InvestmentType { get; set; }
    public string? Platform { get; set; }
    public string? AccountId { get; set; }
    public double? CurrentValue { get; set; }
    public string? Nominee { get; set; }
}

public sealed class InsurancePolicyEntity
{
    public string Id { get; set; } = "";
    public string? PolicyNumber { get; set; }
    public string PolicyType { get; set; } = "";
    public string Coverage { get; set; } = "";
    public double? CoverageAmount { get; set; }
    public string? ExpiryDate { get; set; }
    public string? ClaimContact { get; set; }
    public string? Nominee { get; set; }
}

public sealed class PropertyEntity
{
    public string Id { get; set; } = "";
    public string? OwnershipType { get; set; }
    public string? Address { get; set; }
    public int? LoanLinked { get; set; }
    public string? DocumentsLocation { get; set; }
}

public sealed class EmergencyFundEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? Location { get; set; }
    public double? Amount { get; set; }
    public string? Currency { get; set; }
    public string? AccessInstructions { get; set; }
}

public sealed class NomineeEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Relationship { get; set; }
    public string AssetType { get; set; } = "";
    public string? ContactDetails { get; set; }
}

public sealed class TaxEntryEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? Country { get; set; }
    public string? IncomeType { get; set; }
    public double? TaxPaid { get; set; }
    public int? DeclaredInUK { get; set; }
    public int? Year { get; set; }
}

public sealed class WillEntryEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? Country { get; set; }
    public int? ExistsFlag { get; set; }
    public string? Location { get; set; }
    public string? ExecutorName { get; set; }
    public string? LastUpdated { get; set; }
}

public sealed class DocumentMetadataEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string StoragePath { get; set; } = "";
    public long FileSize { get; set; }
    public string? UploadedAt { get; set; }
}

public sealed class IndiaAssetEntity
{
    public string Id { get; set; } = "";
    public string? AccountType { get; set; }
    public string? Repatriation { get; set; }
    public string? TaxStatus { get; set; }
    public string Category { get; set; } = "";
    public string? Nominee { get; set; }
}

public sealed class UkAssetEntity
{
    public string Id { get; set; } = "";
    public string? Category { get; set; }
    public string? TaxNotes { get; set; }
    public string AccountNumber { get; set; } = "";
    public string? Nominee { get; set; }
}
