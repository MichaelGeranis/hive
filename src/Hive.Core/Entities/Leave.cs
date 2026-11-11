namespace Hive.Core.Entities;

/// <summary>
/// Represents a leave record for a team member.
/// Simple tracking for capacity planning - approvals handled externally (e.g., HiBob).
/// </summary>
public class Leave
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public LeaveType Type { get; private set; }
    public LeaveStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core / serialization
    private Leave() { }

    public Leave(
        Guid directReportId,
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        string? notes = null)
    {
        ValidateDates(startDate, endDate);
        ValidateDirectReportId(directReportId);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        Type = type;
        Status = LeaveStatus.Active;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Notes = notes?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the number of days for this leave (inclusive)
    /// </summary>
    public int DaysCount => (EndDate - StartDate).Days + 1;

    /// <summary>
    /// Gets the number of business days (excluding weekends)
    /// </summary>
    public int BusinessDaysCount
    {
        get
        {
            int count = 0;
            for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Check if this leave overlaps with a date range
    /// </summary>
    public bool OverlapsWith(DateTime start, DateTime end)
    {
        return StartDate <= end && EndDate >= start;
    }

    /// <summary>
    /// Check if this leave includes a specific date
    /// </summary>
    public bool IncludesDate(DateTime date)
    {
        return date.Date >= StartDate && date.Date <= EndDate;
    }

    public void Update(
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        string? notes)
    {
        ValidateDates(startDate, endDate);

        Type = type;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be before start date.");
        }

        if ((endDate - startDate).Days > 365)
        {
            throw new ArgumentException("Leave duration cannot exceed 365 days.");
        }
    }

    private static void ValidateDirectReportId(Guid directReportId)
    {
        if (directReportId == Guid.Empty)
        {
            throw new ArgumentException("Direct report ID is required.", nameof(directReportId));
        }
    }
}
