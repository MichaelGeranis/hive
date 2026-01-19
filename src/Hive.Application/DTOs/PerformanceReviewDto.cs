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
    public string Strengths { get; init; } = string.Empty;
    public string AreasForImprovement { get; init; } = string.Empty;
    public string ManagerNotes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating a new PerformanceReview.
/// </summary>
public record CreatePerformanceReviewDto
{
    public Guid DirectReportId { get; init; }
    public string ReviewPeriod { get; init; } = string.Empty;
    public DateTime ReviewDate { get; init; }

    // Optional content fields that can be set on creation
    public string? Strengths { get; init; }
    public string? AreasForImprovement { get; init; }
    public string? ManagerNotes { get; init; }
    public PerformanceRating? Rating { get; init; }
}

/// <summary>
/// DTO for updating review content.
/// </summary>
public record UpdatePerformanceReviewContentDto
{
    public string Strengths { get; init; } = string.Empty;
    public string AreasForImprovement { get; init; } = string.Empty;
    public string ManagerNotes { get; init; } = string.Empty;
    public PerformanceRating Rating { get; init; }
}
