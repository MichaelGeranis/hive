using System.Globalization;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for managing leave requests.
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IDirectReportRepository _directReportRepository;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IDirectReportRepository directReportRepository)
    {
        _leaveRepository = leaveRepository;
        _directReportRepository = directReportRepository;
    }

    public async Task<LeaveDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return null;

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        return await MapToDtoListAsync(leaves, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var leaves = await _leaveRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtoListAsync(leaves, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LeaveStatus>(status, true, out var leaveStatus))
        {
            throw new ArgumentException($"Invalid leave status: {status}");
        }

        var leaves = await _leaveRepository.GetByStatusAsync(leaveStatus, cancellationToken);
        return await MapToDtoListAsync(leaves, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var leaves = await _leaveRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return await MapToDtoListAsync(leaves, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveDto>> GetUpcomingAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var leaves = await _leaveRepository.GetUpcomingAsync(days, cancellationToken);
        return await MapToDtoListAsync(leaves, cancellationToken);
    }

    public async Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default)
    {
        // Validate direct report exists
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);
        if (directReport == null)
        {
            throw new KeyNotFoundException($"Direct report with ID {dto.DirectReportId} not found.");
        }

        // Parse leave type
        if (!Enum.TryParse<LeaveType>(dto.Type, true, out var leaveType))
        {
            throw new ArgumentException($"Invalid leave type: {dto.Type}");
        }

        // Check for overlapping leaves
        var overlapping = await _leaveRepository.GetOverlappingLeavesAsync(
            dto.DirectReportId, dto.StartDate, dto.EndDate, null, cancellationToken);

        if (overlapping.Any(l => l.Status != LeaveStatus.Cancelled && l.Status != LeaveStatus.Rejected))
        {
            throw new InvalidOperationException("This leave request overlaps with an existing leave.");
        }

        var leave = new Leave(
            dto.DirectReportId,
            leaveType,
            dto.StartDate,
            dto.EndDate,
            dto.Reason,
            dto.Notes);

        var created = await _leaveRepository.AddAsync(leave, cancellationToken);
        return MapToDto(created, directReport.FullName);
    }

    public async Task<LeaveDto> UpdateAsync(Guid id, UpdateLeaveDto dto, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null)
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        if (!Enum.TryParse<LeaveType>(dto.Type, true, out var leaveType))
        {
            throw new ArgumentException($"Invalid leave type: {dto.Type}");
        }

        // Check for overlapping leaves (excluding this one)
        var overlapping = await _leaveRepository.GetOverlappingLeavesAsync(
            leave.DirectReportId, dto.StartDate, dto.EndDate, id, cancellationToken);

        if (overlapping.Any(l => l.Status != LeaveStatus.Cancelled && l.Status != LeaveStatus.Rejected))
        {
            throw new InvalidOperationException("This leave request overlaps with an existing leave.");
        }

        leave.Update(leaveType, dto.StartDate, dto.EndDate, dto.Reason, dto.Notes);
        await _leaveRepository.UpdateAsync(leave, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task<LeaveDto> ApproveAsync(Guid id, ApproveLeaveDto dto, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null)
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        leave.Approve(dto.ApprovedBy);
        await _leaveRepository.UpdateAsync(leave, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task<LeaveDto> RejectAsync(Guid id, RejectLeaveDto dto, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null)
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        leave.Reject(dto.Notes);
        await _leaveRepository.UpdateAsync(leave, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task<LeaveDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null)
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        leave.Cancel();
        await _leaveRepository.UpdateAsync(leave, cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _leaveRepository.ExistsAsync(id, cancellationToken))
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        await _leaveRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<TeamLeaveOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken = default)
    {
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);

        var activeLeaves = allLeaves.Where(l => l.Status == LeaveStatus.Approved).ToList();
        var currentLeaves = activeLeaves.Where(l => l.IncludesDate(today)).ToList();
        var thisWeekLeaves = activeLeaves.Where(l => l.OverlapsWith(today, weekEnd)).ToList();

        var upcomingLeaves = await _leaveRepository.GetUpcomingAsync(30, cancellationToken);
        var monthlyTrend = await GetMonthlyTrendAsync(6, cancellationToken);

        return new TeamLeaveOverviewDto
        {
            TotalLeaveRequests = allLeaves.Count,
            PendingRequests = allLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedRequests = allLeaves.Count(l => l.Status == LeaveStatus.Approved),
            TeamMembersOnLeaveToday = currentLeaves.Select(l => l.DirectReportId).Distinct().Count(),
            TeamMembersOnLeaveThisWeek = thisWeekLeaves.Select(l => l.DirectReportId).Distinct().Count(),
            UpcomingLeaves = (await MapToDtoListAsync(upcomingLeaves.Take(5).ToList(), cancellationToken)).ToList(),
            CurrentLeaves = (await MapToDtoListAsync(currentLeaves, cancellationToken)).ToList(),
            MonthlyTrend = monthlyTrend.ToList()
        };
    }

    public async Task<IReadOnlyList<MonthlyLeaveSummaryDto>> GetMonthlyTrendAsync(int months = 12, CancellationToken cancellationToken = default)
    {
        var result = new List<MonthlyLeaveSummaryDto>();
        var today = DateTime.UtcNow.Date;

        for (int i = months - 1; i >= 0; i--)
        {
            var targetDate = today.AddMonths(-i);
            var year = targetDate.Year;
            var month = targetDate.Month;

            var monthLeaves = await _leaveRepository.GetByMonthAsync(year, month, cancellationToken);
            var approvedLeaves = monthLeaves.Where(l => l.Status == LeaveStatus.Approved).ToList();

            var byType = approvedLeaves
                .GroupBy(l => l.Type)
                .Select(g => new LeaveTypeSummaryDto
                {
                    Type = g.Key.ToString(),
                    Count = g.Count(),
                    TotalDays = g.Sum(l => l.DaysCount),
                    TotalBusinessDays = g.Sum(l => l.BusinessDaysCount)
                })
                .ToList();

            result.Add(new MonthlyLeaveSummaryDto
            {
                Year = year,
                Month = month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                TotalLeaves = approvedLeaves.Count,
                TotalDays = approvedLeaves.Sum(l => l.DaysCount),
                TotalBusinessDays = approvedLeaves.Sum(l => l.BusinessDaysCount),
                ByType = byType
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetTeamBalancesAsync(int year, CancellationToken cancellationToken = default)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var startOfYear = new DateTime(year, 1, 1);
        var endOfYear = new DateTime(year, 12, 31);
        var allLeaves = await _leaveRepository.GetByDateRangeAsync(startOfYear, endOfYear, cancellationToken);

        var result = new List<LeaveBalanceDto>();

        foreach (var dr in directReports)
        {
            var drLeaves = allLeaves.Where(l => l.DirectReportId == dr.Id).ToList();
            var approved = drLeaves.Where(l => l.Status == LeaveStatus.Approved).ToList();
            var pending = drLeaves.Where(l => l.Status == LeaveStatus.Pending).ToList();

            result.Add(new LeaveBalanceDto
            {
                DirectReportId = dr.Id,
                DirectReportName = dr.FullName,
                Year = year,
                PtoUsed = approved.Where(l => l.Type == LeaveType.PTO).Sum(l => l.BusinessDaysCount),
                VacationUsed = approved.Where(l => l.Type == LeaveType.Vacation).Sum(l => l.BusinessDaysCount),
                SickLeaveUsed = approved.Where(l => l.Type == LeaveType.SickLeave).Sum(l => l.BusinessDaysCount),
                TotalUsed = approved.Sum(l => l.BusinessDaysCount),
                PendingDays = pending.Sum(l => l.BusinessDaysCount)
            });
        }

        return result;
    }

    private static LeaveDto MapToDto(Leave leave, string directReportName)
    {
        return new LeaveDto
        {
            Id = leave.Id,
            DirectReportId = leave.DirectReportId,
            DirectReportName = directReportName,
            Type = leave.Type.ToString(),
            Status = leave.Status.ToString(),
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            DaysCount = leave.DaysCount,
            BusinessDaysCount = leave.BusinessDaysCount,
            Reason = leave.Reason,
            Notes = leave.Notes,
            CreatedAt = leave.CreatedAt,
            UpdatedAt = leave.UpdatedAt,
            ApprovedAt = leave.ApprovedAt,
            ApprovedBy = leave.ApprovedBy
        };
    }

    private async Task<IReadOnlyList<LeaveDto>> MapToDtoListAsync(
        IReadOnlyList<Leave> leaves,
        CancellationToken cancellationToken)
    {
        var directReportIds = leaves.Select(l => l.DirectReportId).Distinct().ToList();
        var directReports = new Dictionary<Guid, string>();

        foreach (var id in directReportIds)
        {
            var dr = await _directReportRepository.GetByIdAsync(id, cancellationToken);
            directReports[id] = dr?.FullName ?? "Unknown";
        }

        return leaves.Select(l => MapToDto(l, directReports[l.DirectReportId])).ToList();
    }
}
