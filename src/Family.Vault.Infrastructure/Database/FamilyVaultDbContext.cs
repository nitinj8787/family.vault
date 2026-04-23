using Family.Vault.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Family.Vault.Infrastructure.Database;

public sealed class FamilyVaultDbContext(DbContextOptions<FamilyVaultDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();
    public DbSet<ContactEntity> Contacts => Set<ContactEntity>();
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<BankAccountEntity> BankAccounts => Set<BankAccountEntity>();
    public DbSet<InvestmentEntity> Investments => Set<InvestmentEntity>();
    public DbSet<InsurancePolicyEntity> InsurancePolicies => Set<InsurancePolicyEntity>();
    public DbSet<PropertyEntity> Properties => Set<PropertyEntity>();
    public DbSet<EmergencyFundEntity> EmergencyFunds => Set<EmergencyFundEntity>();
    public DbSet<NomineeEntity> Nominees => Set<NomineeEntity>();
    public DbSet<TaxEntryEntity> TaxEntries => Set<TaxEntryEntity>();
    public DbSet<WillEntryEntity> WillEntries => Set<WillEntryEntity>();
    public DbSet<DocumentMetadataEntity> Documents => Set<DocumentMetadataEntity>();
    public DbSet<IndiaAssetEntity> IndiaAssets => Set<IndiaAssetEntity>();
    public DbSet<UkAssetEntity> UkAssets => Set<UkAssetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<ProfileEntity>(entity =>
        {
            entity.ToTable("Profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.ChildrenJson).HasDefaultValue("[]").IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactEntity>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetEntity>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetType).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AssetType);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankAccountEntity>(entity =>
        {
            entity.ToTable("BankAccounts");
            entity.HasKey(e => e.Id);
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<BankAccountEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvestmentEntity>(entity =>
        {
            entity.ToTable("Investments");
            entity.HasKey(e => e.Id);
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<InvestmentEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InsurancePolicyEntity>(entity =>
        {
            entity.ToTable("InsurancePolicies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyType).HasDefaultValue("").IsRequired();
            entity.Property(e => e.Coverage).HasDefaultValue("").IsRequired();
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<InsurancePolicyEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyEntity>(entity =>
        {
            entity.ToTable("Properties");
            entity.HasKey(e => e.Id);
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<PropertyEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmergencyFundEntity>(entity =>
        {
            entity.ToTable("EmergencyFunds");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NomineeEntity>(entity =>
        {
            entity.ToTable("Nominees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.AssetType).HasDefaultValue("").IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaxEntryEntity>(entity =>
        {
            entity.ToTable("TaxEntries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WillEntryEntity>(entity =>
        {
            entity.ToTable("WillEntries");
            entity.HasKey(e => e.Id);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentMetadataEntity>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Description).HasDefaultValue("").IsRequired();
            entity.Property(e => e.StoragePath).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<UserEntity>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IndiaAssetEntity>(entity =>
        {
            entity.ToTable("IndiaAssets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasDefaultValue("").IsRequired();
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<IndiaAssetEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UkAssetEntity>(entity =>
        {
            entity.ToTable("UkAssets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).HasDefaultValue("").IsRequired();
            entity.HasOne<AssetEntity>().WithOne().HasForeignKey<UkAssetEntity>(e => e.Id).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
