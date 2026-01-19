namespace Hive.Core.Entities;

/// <summary>
/// Represents a performance review for a direct report.
/// </summary>
public class PerformanceReview
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public string ReviewPeriod { get; private set; } = string.Empty;
    public DateTime ReviewDate { get; private set; }
    public PerformanceRating Rating { get; private set; }
    public string Strengths { get; private set; } = string.Empty;
    public string AreasForImprovement { get; private set; } = string.Empty;
    public string ManagerNotes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core / serialization
    private PerformanceReview() { }

    public PerformanceReview(
        Guid directReportId,
        string reviewPeriod,
        DateTime reviewDate)
    {
        ValidateDirectReportId(directReportId);
        ValidateReviewPeriod(reviewPeriod);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        ReviewPeriod = reviewPeriod.Trim();
        ReviewDate = reviewDate;
        Rating = PerformanceRating.NotRated;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the review content.
    /// </summary>
    public void UpdateContent(
        string strengths,
        string areasForImprovement,
        string managerNotes,
        PerformanceRating rating)
    {
        Strengths = strengths?.Trim() ?? string.Empty;
        AreasForImprovement = areasForImprovement?.Trim() ?? string.Empty;
        ManagerNotes = managerNotes?.Trim() ?? string.Empty;
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateDirectReportId(Guid directReportId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("DirectReportId cannot be empty.", nameof(directReportId));
        }
    }

    private static void ValidateReviewPeriod(string reviewPeriod)
    {
        if (string.IsNullOrWhiteSpace(reviewPeriod))
        {
            throw new ArgumentException("Review period cannot be empty.", nameof(reviewPeriod));
        }

        if (reviewPeriod.Length > 50)
        {
            throw new ArgumentException("Review period cannot exceed 50 characters.", nameof(reviewPeriod));
        }
    }
}
