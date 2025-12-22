using Hive.Core.Entities;

namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for PerformanceReview - used for API responses.
/// </summary>
public record PerformanceReviewDto
{
    public Guid Id { get; init; }
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public string ReviewPeriod { get; init; } = string.Empty;
    public DateTime ReviewDate { get; init; }
    public PerformanceRating Rating { get; init; }
    public string RatingDescription { get; init; } = string.Empty;
    public ReviewStatus Status { get; init; }
    public string StatusDescription { get; init; } = string.Empty;
    public string Strengths { get; init; } = string.Empty;
    public string AreasForImprovement { get; init; } = string.Empty;
    public string GoalsForNextPeriod { get; init; } = string.Empty;
    public string ManagerNotes { get; init; } = string.Empty;
    public string EmployeeSelfAssessment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
}

/// <summary>
/// DTO for creating a new PerformanceReview.
/// </summary>
public record CreatePerformanceReviewDto
{
    public Guid DirectReportId { get; init; }
    public string ReviewPeriod { get; init; } = string.Empty;
    public DateTime ReviewDate { get; init; }
}

/// <summary>
/// DTO for updating review content.
/// </summary>
public record UpdatePerformanceReviewContentDto
{
    public string Strengths { get; init; } = string.Empty;
    public string AreasForImprovement { get; init; } = string.Empty;
    public string GoalsForNextPeriod { get; init; } = string.Empty;
    public string ManagerNotes { get; init; } = string.Empty;
    public PerformanceRating Rating { get; init; }
}

/// <summary>
/// DTO for updating employee self-assessment.
/// </summary>
public record UpdateSelfAssessmentDto
{
    public string SelfAssessment { get; init; } = string.Empty;
}
