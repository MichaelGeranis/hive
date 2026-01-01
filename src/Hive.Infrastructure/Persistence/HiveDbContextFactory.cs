using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for HiveDbContext to support EF Core migrations.
/// </summary>
public class HiveDbContextFactory : IDesignTimeDbContextFactory<HiveDbContext>
{
    public HiveDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HiveDbContext>();
        
        // Use a temporary SQLite database for migrations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseSqlite("Data Source=hive_migrations.db");
        
        return new HiveDbContext(optionsBuilder.Options);
    }
}
