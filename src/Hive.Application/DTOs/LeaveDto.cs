using System.ComponentModel.DataAnnotations;

namespace Hive.Application.DTOs;

/// <summary>
/// DTO for displaying leave information
/// </summary>
public class LeaveDto
{
    public Guid Id { get; set; }
    public Guid DirectReportId { get; set; }
    public string DirectReportName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount { get; set; }
    public int BusinessDaysCount { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// DTO for creating a new leave request
/// </summary>
public class CreateLeaveDto
{
    [Required]
    public Guid DirectReportId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating a leave request
/// </summary>
public class UpdateLeaveDto
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for approving a leave request
/// </summary>
public class ApproveLeaveDto
{
    [Required]
    [MaxLength(100)]
    public string ApprovedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for rejecting a leave request
/// </summary>
public class RejectLeaveDto
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Summary of leaves by type for a specific period
/// </summary>
public class LeaveTypeSummaryDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public int TotalDays { get; set; }
    public int TotalBusinessDays { get; set; }
}

/// <summary>
/// Monthly leave statistics
/// </summary>
public class MonthlyLeaveSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalLeaves { get; set; }
    public int TotalDays { get; set; }
    public int TotalBusinessDays { get; set; }
    public List<LeaveTypeSummaryDto> ByType { get; set; } = new();
}

/// <summary>
/// Leave balance for a team member
/// </summary>
public class LeaveBalanceDto
{
    public Guid DirectReportId { get; set; }
    public string DirectReportName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PtoUsed { get; set; }
    public int VacationUsed { get; set; }
    public int SickLeaveUsed { get; set; }
    public int TotalUsed { get; set; }
    public int PendingDays { get; set; }
}

/// <summary>
/// Team leave overview for dashboard
/// </summary>
public class TeamLeaveOverviewDto
{
    public int TotalLeaveRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int TeamMembersOnLeaveToday { get; set; }
    public int TeamMembersOnLeaveThisWeek { get; set; }
    public List<LeaveDto> UpcomingLeaves { get; set; } = new();
    public List<LeaveDto> CurrentLeaves { get; set; } = new();
    public List<MonthlyLeaveSummaryDto> MonthlyTrend { get; set; } = new();
}
