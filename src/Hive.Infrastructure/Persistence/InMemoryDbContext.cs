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
    public ConcurrentDictionary<Guid, PerformanceReview> PerformanceReviews { get; } = new();

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

        // Seed sample performance reviews
        SeedPerformanceReviews();
    }

    private void SeedPerformanceReviews()
    {
        var directReportIds = DirectReports.Keys.ToList();
        if (directReportIds.Count == 0) return;

        // Create a sample completed review for first direct report
        var review1 = new PerformanceReview(directReportIds[0], "2024 Annual Review", new DateTime(2024, 12, 15));
        review1.UpdateContent(
            "Strong technical leadership and mentoring skills",
            "Could improve delegation and documentation",
            "Lead architecture redesign initiative",
            "Excellent year overall",
            PerformanceRating.ExceedsExpectations);
        review1.UpdateSelfAssessment("I feel I've grown significantly this year in my technical abilities.");
        review1.Submit();
        review1.Acknowledge();
        review1.Complete();
        PerformanceReviews.TryAdd(review1.Id, review1);

        // Create a draft review for second direct report
        if (directReportIds.Count > 1)
        {
            var review2 = new PerformanceReview(directReportIds[1], "2024 Annual Review", new DateTime(2024, 12, 15));
            PerformanceReviews.TryAdd(review2.Id, review2);
        }
    }
}
