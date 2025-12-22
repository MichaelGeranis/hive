namespace Hive.Core.Entities;

/// <summary>
/// Represents the rating given during a performance review.
/// </summary>
public enum PerformanceRating
{
    NotRated = 0,
    NeedsImprovement = 1,
    MeetsExpectations = 2,
    ExceedsExpectations = 3,
    Outstanding = 4
}

/// <summary>
/// Represents the status of a performance review.
/// </summary>
public enum ReviewStatus
{
    Draft = 0,
    Submitted = 1,
    Acknowledged = 2,
    Completed = 3
}
