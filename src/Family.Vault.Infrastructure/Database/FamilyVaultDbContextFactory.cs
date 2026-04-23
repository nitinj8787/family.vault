using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Family.Vault.Infrastructure.Database;

public sealed class FamilyVaultDbContextFactory : IDesignTimeDbContextFactory<FamilyVaultDbContext>
{
    public FamilyVaultDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FamilyVaultDbContext>();
        optionsBuilder.UseSqlite("Data Source=familyvault.db");
        return new FamilyVaultDbContext(optionsBuilder.Options);
    }
}
