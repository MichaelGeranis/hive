namespace Hive.Core.Entities;

/// <summary>
/// Represents a performance review for a direct report.
/// Contains critical business rules for the review process.
/// </summary>
public class PerformanceReview
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public string ReviewPeriod { get; private set; } = string.Empty;
    public DateTime ReviewDate { get; private set; }
    public PerformanceRating Rating { get; private set; }
    public ReviewStatus Status { get; private set; }
    public string Strengths { get; private set; } = string.Empty;
    public string AreasForImprovement { get; private set; } = string.Empty;
    public string GoalsForNextPeriod { get; private set; } = string.Empty;
    public string ManagerNotes { get; private set; } = string.Empty;
    public string EmployeeSelfAssessment { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }

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
        Status = ReviewStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the review content. Only allowed when status is Draft.
    /// </summary>
    public void UpdateContent(
        string strengths,
        string areasForImprovement,
        string goalsForNextPeriod,
        string managerNotes,
        PerformanceRating rating)
    {
        EnsureCanEdit();

        Strengths = strengths?.Trim() ?? string.Empty;
        AreasForImprovement = areasForImprovement?.Trim() ?? string.Empty;
        GoalsForNextPeriod = goalsForNextPeriod?.Trim() ?? string.Empty;
        ManagerNotes = managerNotes?.Trim() ?? string.Empty;
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the employee self-assessment.
    /// </summary>
    public void UpdateSelfAssessment(string selfAssessment)
    {
        if (Status == ReviewStatus.Completed)
        {
            throw new InvalidOperationException("Cannot modify a completed review.");
        }

        EmployeeSelfAssessment = selfAssessment?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Submits the review for employee acknowledgment.
    /// </summary>
    public void Submit()
    {
        if (Status != ReviewStatus.Draft)
        {
            throw new InvalidOperationException("Only draft reviews can be submitted.");
        }

        if (Rating == PerformanceRating.NotRated)
        {
            throw new InvalidOperationException("Rating must be set before submitting.");
        }

        Status = ReviewStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the review as acknowledged by the employee.
    /// </summary>
    public void Acknowledge()
    {
        if (Status != ReviewStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted reviews can be acknowledged.");
        }

        Status = ReviewStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes and finalizes the review.
    /// </summary>
    public void Complete()
    {
        if (Status != ReviewStatus.Acknowledged)
        {
            throw new InvalidOperationException("Only acknowledged reviews can be completed.");
        }

        Status = ReviewStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reopens a submitted review back to draft status.
    /// </summary>
    public void Reopen()
    {
        if (Status == ReviewStatus.Completed)
        {
            throw new InvalidOperationException("Completed reviews cannot be reopened.");
        }

        Status = ReviewStatus.Draft;
        SubmittedAt = null;
        AcknowledgedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureCanEdit()
    {
        if (Status != ReviewStatus.Draft)
        {
            throw new InvalidOperationException("Review content can only be edited in Draft status.");
        }
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
