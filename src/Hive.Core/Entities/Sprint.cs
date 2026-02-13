using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// Represents a sprint with parsed components from sprint names.
/// Sprint names follow the template: {TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}
/// Example: LP_4Q25_S6 (Team LP, Q4 2025, Sprint 6)
/// </summary>
public class Sprint
{
    private static readonly Regex SprintNamePattern = new(
        @"^(\w+)_(\d)Q(\d{2})_S(\d+)$",
        RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TeamName { get; private set; } = string.Empty;
    public int Quarter { get; private set; }
    public int Year { get; private set; }
    public int SprintNumber { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Sprint() { }

    public Sprint(string name)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        ParseSprintName(name);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Parses sprint name following template: {TeamName}_{Quarter}Q{YearShort}_S{SprintNumber}
    /// Example: LP_4Q25_S6 -> TeamName=LP, Quarter=4, Year=2025, SprintNumber=6
    /// </summary>
    private void ParseSprintName(string name)
    {
        var match = SprintNamePattern.Match(name.Trim());

        if (match.Success)
        {
            TeamName = match.Groups[1].Value;
            Quarter = int.Parse(match.Groups[2].Value);
            Year = 2000 + int.Parse(match.Groups[3].Value);
            SprintNumber = int.Parse(match.Groups[4].Value);
        }
        else
        {
            // Fallback for non-standard sprint names
            TeamName = name.Trim();
            Quarter = 0;
            Year = 0;
            SprintNumber = 0;
        }
    }

    public void Update(string name)
    {
        ValidateName(name);

        Name = name.Trim();
        ParseSprintName(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDates(DateTime? startDate, DateTime? endDate)
    {
        ValidateDates(startDate, endDate);

        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the sort order for this sprint based on year, quarter, and sprint number.
    /// Higher values are more recent sprints.
    /// This is a fallback for sprints without dates.
    /// </summary>
    public int GetSortOrder()
    {
        // Format: YYYYQSS (e.g., 2025406 for Q4 2025 Sprint 6)
        return (Year * 1000) + (Quarter * 100) + SprintNumber;
    }

    /// <summary>
    /// Gets a comparable key for ordering sprints.
    /// Returns a tuple where actual dates take precedence over calculated dates.
    /// </summary>
    public (int Priority, DateTime Date) GetOrderingKey()
    {
        if (StartDate.HasValue)
            return (2, StartDate.Value); // Highest priority: actual dates

        if (Year > 0)
            return (1, GetEstimatedStartDate()); // Medium priority: calculated from standard name

        return (0, DateTime.MinValue); // Lowest priority: non-standard names
    }

    /// <summary>
    /// Determines if this sprint is before another sprint.
    /// Prioritizes actual start dates, falls back to sort order for non-standard sprint names.
    /// </summary>
    public bool IsBefore(Sprint other)
    {
        // If both sprints have actual start dates, use those
        if (StartDate.HasValue && other.StartDate.HasValue)
            return StartDate.Value < other.StartDate.Value;

        // If both are standard sprints (Year > 0), use estimated dates
        if (Year > 0 && other.Year > 0)
            return GetEstimatedStartDate() < other.GetEstimatedStartDate();

        // Fall back to sort order comparison for non-standard names
        return GetSortOrder() < other.GetSortOrder();
    }

    /// <summary>
    /// Determines if this sprint is after another sprint.
    /// Prioritizes actual start dates, falls back to sort order for non-standard sprint names.
    /// </summary>
    public bool IsAfter(Sprint other)
    {
        // If both sprints have actual start dates, use those
        if (StartDate.HasValue && other.StartDate.HasValue)
            return StartDate.Value > other.StartDate.Value;

        // If both are standard sprints (Year > 0), use estimated dates
        if (Year > 0 && other.Year > 0)
            return GetEstimatedStartDate() > other.GetEstimatedStartDate();

        // Fall back to sort order comparison for non-standard names
        return GetSortOrder() > other.GetSortOrder();
    }

    /// <summary>
    /// Gets the estimated start date for this sprint.
    /// Uses actual StartDate if available, otherwise calculates from year, quarter, and sprint number.
    /// Returns DateTime.MinValue for non-standard sprint names (Year=0).
    /// </summary>
    public DateTime GetEstimatedStartDate()
    {
        if (StartDate.HasValue)
            return StartDate.Value;

        // Non-standard sprint names (Year=0) get DateTime.MinValue
        if (Year == 0)
            return DateTime.MinValue;

        // Calculate based on year, quarter, and sprint number
        // Quarter start month: Q1=Jan(0), Q2=Apr(3), Q3=Jul(6), Q4=Oct(9)
        var quarterStartMonth = (Quarter - 1) * 3;
        // Each sprint is ~2 weeks, so sprint 1 starts day 1, sprint 2 starts day 15, etc.
        var dayOfQuarter = 1 + (SprintNumber - 1) * 14;

        return new DateTime(Year, quarterStartMonth + 1, 1).AddDays(dayOfQuarter - 1);
    }

    /// <summary>
    /// Gets the estimated end date for this sprint.
    /// Uses actual EndDate if available, otherwise calculates as 13 days after start (2-week sprint).
    /// Returns DateTime.MinValue for non-standard sprint names (Year=0).
    /// </summary>
    public DateTime GetEstimatedEndDate()
    {
        if (EndDate.HasValue)
            return EndDate.Value;

        var startDate = GetEstimatedStartDate();
        if (startDate == DateTime.MinValue)
            return DateTime.MinValue;

        return startDate.AddDays(13);
    }

    /// <summary>
    /// Determines if the given date falls within this sprint's date range.
    /// </summary>
    public bool ContainsDate(DateTime date)
    {
        var start = GetEstimatedStartDate().Date;
        var end = GetEstimatedEndDate().Date;
        return date.Date >= start && date.Date <= end;
    }

    /// <summary>
    /// Determines if this sprint is in the past relative to the given date.
    /// </summary>
    public bool IsPast(DateTime referenceDate)
    {
        return GetEstimatedEndDate().Date < referenceDate.Date;
    }

    /// <summary>
    /// Determines if this sprint is in the future relative to the given date.
    /// </summary>
    public bool IsFuture(DateTime referenceDate)
    {
        return GetEstimatedStartDate().Date > referenceDate.Date;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sprint name cannot be empty.", nameof(name));
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Sprint name cannot exceed 100 characters.", nameof(name));
        }
    }

    private static void ValidateDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("Start date cannot be after end date.", nameof(startDate));
        }
    }
}
