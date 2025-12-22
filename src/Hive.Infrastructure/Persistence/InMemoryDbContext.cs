using Hive.Core.Entities;
using System.Collections.Concurrent;

namespace Hive.Infrastructure.Persistence;

/// <summary>
/// In-memory database context for development/testing.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class InMemoryDbContext
{
    public ConcurrentDictionary<Guid, DirectReport> DirectReports { get; } = new();

    /// <summary>
    /// Seeds the database with sample data for development.
    /// </summary>
    public void SeedData()
    {
        var sampleReports = new[]
        {
            new DirectReport("Alice", "Johnson", "alice.johnson@company.com", "Senior Software Engineer", "Engineering", new DateTime(2022, 3, 15)),
            new DirectReport("Bob", "Smith", "bob.smith@company.com", "Software Engineer", "Engineering", new DateTime(2023, 1, 10)),
            new DirectReport("Carol", "Williams", "carol.williams@company.com", "Staff Engineer", "Engineering", new DateTime(2021, 6, 1))
        };

        foreach (var report in sampleReports)
        {
            DirectReports.TryAdd(report.Id, report);
        }
    }
}
