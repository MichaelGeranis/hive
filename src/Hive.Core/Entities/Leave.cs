namespace Hive.Core.Entities;

/// <summary>
/// Represents a leave request for a team member.
/// Tracks PTO, vacation, sick leave, and other time off.
/// </summary>
public class Leave
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }
    public LeaveType Type { get; private set; }
    public LeaveStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }

    // Private constructor for EF Core / serialization
    private Leave() { }

    public Leave(
        Guid directReportId,
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        string? reason = null,
        string? notes = null)
    {
        ValidateDates(startDate, endDate);
        ValidateDirectReportId(directReportId);

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        Type = type;
        Status = LeaveStatus.Pending;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Reason = reason?.Trim();
        Notes = notes?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the number of days for this leave request (inclusive)
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
        string? reason,
        string? notes)
    {
        if (Status == LeaveStatus.Approved)
        {
            throw new InvalidOperationException("Cannot modify an approved leave request.");
        }

        ValidateDates(startDate, endDate);

        Type = type;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Reason = reason?.Trim();
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(string approvedBy)
    {
        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve a leave request with status: {Status}");
        }

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException("Approver name is required.", nameof(approvedBy));
        }

        Status = LeaveStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject a leave request with status: {Status}");
        }

        Status = LeaveStatus.Rejected;
        if (notes != null)
        {
            Notes = notes.Trim();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == LeaveStatus.Cancelled)
        {
            throw new InvalidOperationException("Leave request is already cancelled.");
        }

        Status = LeaveStatus.Cancelled;
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
