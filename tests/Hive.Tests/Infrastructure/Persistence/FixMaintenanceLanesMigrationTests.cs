using Hive.Core.Entities;
using Hive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hive.Tests.Infrastructure.Persistence;

/// <summary>
/// The FixMaintenanceLanes migration puts back the Maintenance lanes SQLite stored as Product
/// roadmap, and leaves every other initiative alone.
/// </summary>
public class FixMaintenanceLanesMigrationTests : IDisposable
{
    private const string MigrationBefore = "20260929081406_RemoveSupportLabels";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"hive-migration-test-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Up_LaneStoredAsProductRoadmap_BecomesMaintenance()
    {
        // Arrange — a database as it was before the fix, with the lane the bug wrote
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBefore);
        var quarter = new Quarter(2026, 4);
        var maintenance = Initiative.CreateBucket(quarter.Id, WorkType.Maintenance);
        var techRoadmap = Initiative.CreateBucket(quarter.Id, WorkType.TechRoadmap);
        var option = new Initiative(quarter.Id, "Rights search", "#3B82F6", workType: WorkType.ProductRoadmap);
        context.AddRange(quarter, maintenance, techRoadmap, option);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlAsync($"UPDATE Initiatives SET WorkType = 1 WHERE Id = {maintenance.Id}");

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var byName = (await context.Set<Initiative>().AsNoTracking().ToListAsync())
            .ToDictionary(i => i.Name, i => i.WorkType);
        byName[maintenance.Name].Should().Be(WorkType.Maintenance);
        byName[techRoadmap.Name].Should().Be(WorkType.TechRoadmap);
        byName["Rights search"].Should().Be(WorkType.ProductRoadmap, "an option can be product roadmap");
    }

    private HiveDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HiveDbContext>()
            .UseSqlite($"Data Source={_databasePath};Pooling=False")
            .Options);

    public void Dispose()
    {
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (File.Exists(file)) File.Delete(file);
        }
        GC.SuppressFinalize(this);
    }
}
