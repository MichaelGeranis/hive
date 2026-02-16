using System.Globalization;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for managing leave records.
/// Simple tracking for capacity planning - approvals handled externally (e.g., HiBob).
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;
    private readonly ISprintCapacityService _sprintCapacityService;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService,
        ISprintCapacityService sprintCapacityService)
    {
        _leaveRepository = leaveRepository;
        _directReportRepository = directReportRepository;
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _sprintCapacityService = sprintCapacityService ?? throw new ArgumentNullException(nameof(sprintCapacityService));
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

        if (overlapping.Any())
        {
            throw new InvalidOperationException("This leave overlaps with an existing leave record.");
        }

        var leave = new Leave(
            dto.DirectReportId,
            leaveType,
            dto.StartDate,
            dto.EndDate,
            dto.Notes);

        var created = await _leaveRepository.AddAsync(leave, cancellationToken);

        await _sprintCapacityService.RecalculateForDateRangeAsync(leave.StartDate, leave.EndDate, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Leave,
            created.Id,
            $"Leave - {leaveType} ({created.StartDate:MMM d} - {created.EndDate:MMM d})",
            "Leave request created",
            cancellationToken);

        return MapToDto(created, directReport.FullName);
    }

    public async Task<CreatePublicHolidayResultDto> CreatePublicHolidayAsync(
        CreatePublicHolidayLeaveDto dto,
        CancellationToken cancellationToken = default)
    {
        // Get all active direct reports
        var allDirectReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var activeDirectReports = allDirectReports
            .Where(dr => dr.IsDirect)
            .ToList();

        if (activeDirectReports.Count == 0)
        {
            throw new InvalidOperationException("No active team members found to create public holiday for.");
        }

        // Pre-validate all team members for overlapping leaves
        var overlappingMembers = new List<string>();
        foreach (var directReport in activeDirectReports)
        {
            var overlapping = await _leaveRepository.GetOverlappingLeavesAsync(
                directReport.Id, dto.StartDate, dto.EndDate, null, cancellationToken);

            if (overlapping.Any())
            {
                overlappingMembers.Add(directReport.FullName);
            }
        }

        if (overlappingMembers.Any())
        {
            throw new InvalidOperationException(
                $"Cannot create public holiday. Overlapping leaves exist for: {string.Join(", ", overlappingMembers)}");
        }

        // Create leaves for all team members
        var createdLeaves = new List<Leave>();
        var notes = string.IsNullOrWhiteSpace(dto.Notes)
            ? $"Public Holiday: {dto.Name}"
            : $"Public Holiday: {dto.Name} - {dto.Notes}";

        foreach (var directReport in activeDirectReports)
        {
            var leave = new Leave(
                directReport.Id,
                LeaveType.PublicHoliday,
                dto.StartDate,
                dto.EndDate,
                notes);

            var created = await _leaveRepository.AddAsync(leave, cancellationToken);
            createdLeaves.Add(created);
        }

        // Recalculate sprint capacity once for the date range
        await _sprintCapacityService.RecalculateForDateRangeAsync(
            dto.StartDate, dto.EndDate, cancellationToken);

        // Log a single activity for the public holiday
        var firstLeaveId = createdLeaves.First().Id;
        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Leave,
            firstLeaveId,
            $"Public Holiday - {dto.Name}",
            $"Created for {createdLeaves.Count} team members ({dto.StartDate:MMM d} - {dto.EndDate:MMM d})",
            cancellationToken);

        // Map to DTOs
        var leaveDtos = new List<LeaveDto>();
        foreach (var leave in createdLeaves)
        {
            var dr = activeDirectReports.First(d => d.Id == leave.DirectReportId);
            leaveDtos.Add(MapToDto(leave, dr.FullName));
        }

        return new CreatePublicHolidayResultDto
        {
            TotalCreated = createdLeaves.Count,
            TeamMembersAffected = activeDirectReports.Select(dr => dr.FullName).ToList(),
            CreatedLeaves = leaveDtos
        };
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

        if (overlapping.Any())
        {
            throw new InvalidOperationException("This leave overlaps with an existing leave record.");
        }

        var oldStartDate = leave.StartDate;
        var oldEndDate = leave.EndDate;

        leave.Update(leaveType, dto.StartDate, dto.EndDate, dto.Notes);
        await _leaveRepository.UpdateAsync(leave, cancellationToken);

        // Recalculate for the union of old and new date ranges
        var rangeStart = oldStartDate < dto.StartDate ? oldStartDate : dto.StartDate;
        var rangeEnd = oldEndDate > dto.EndDate ? oldEndDate : dto.EndDate;
        await _sprintCapacityService.RecalculateForDateRangeAsync(rangeStart, rangeEnd, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Leave,
            leave.Id,
            $"Leave - {leaveType} ({leave.StartDate:MMM d} - {leave.EndDate:MMM d})",
            "Leave request updated",
            cancellationToken);

        var directReport = await _directReportRepository.GetByIdAsync(leave.DirectReportId, cancellationToken);
        return MapToDto(leave, directReport?.FullName ?? "Unknown");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave is null)
        {
            throw new KeyNotFoundException($"Leave with ID {id} not found.");
        }

        var leaveStartDate = leave.StartDate;
        var leaveEndDate = leave.EndDate;

        await _leaveRepository.DeleteAsync(id, cancellationToken);

        await _sprintCapacityService.RecalculateForDateRangeAsync(leaveStartDate, leaveEndDate, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Leave,
            id,
            $"Leave - {leave.Type} ({leave.StartDate:MMM d} - {leave.EndDate:MMM d})",
            "Leave request deleted",
            cancellationToken);
    }

    public async Task<TeamLeaveOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken = default)
    {
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);

        // Filter to only include leaves from direct reports (not indirect reports)
        var allReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var directReportIds = allReports.Where(dr => dr.IsDirect).Select(dr => dr.Id).ToHashSet();
        var directReportLeaves = allLeaves.Where(l => directReportIds.Contains(l.DirectReportId)).ToList();

        var currentLeaves = directReportLeaves.Where(l => l.IncludesDate(today)).ToList();
        var thisWeekLeaves = directReportLeaves.Where(l => l.OverlapsWith(today, weekEnd)).ToList();

        var upcomingLeaves = await _leaveRepository.GetUpcomingAsync(30, cancellationToken);
        var directReportUpcomingLeaves = upcomingLeaves.Where(l => directReportIds.Contains(l.DirectReportId)).ToList();
        var monthlyTrend = await GetMonthlyTrendAsync(6, cancellationToken);

        return new TeamLeaveOverviewDto
        {
            TotalLeaveRecords = directReportLeaves.Count,
            TeamMembersOnLeaveToday = currentLeaves.Select(l => l.DirectReportId).Distinct().Count(),
            TeamMembersOnLeaveThisWeek = thisWeekLeaves.Select(l => l.DirectReportId).Distinct().Count(),
            UpcomingLeaves = (await MapToDtoListAsync(directReportUpcomingLeaves.Take(5).ToList(), cancellationToken)).ToList(),
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

            var byType = monthLeaves
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
                TotalLeaves = monthLeaves.Count,
                TotalDays = monthLeaves.Sum(l => l.DaysCount),
                TotalBusinessDays = monthLeaves.Sum(l => l.BusinessDaysCount),
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

            result.Add(new LeaveBalanceDto
            {
                DirectReportId = dr.Id,
                DirectReportName = dr.FullName,
                Year = year,
                VacationUsed = drLeaves.Where(l => l.Type == LeaveType.Vacation).Sum(l => l.BusinessDaysCount),
                SickLeaveUsed = drLeaves.Where(l => l.Type == LeaveType.Sick).Sum(l => l.BusinessDaysCount),
                OtherUsed = drLeaves.Where(l => l.Type == LeaveType.Other).Sum(l => l.BusinessDaysCount),
                TotalUsed = drLeaves.Sum(l => l.BusinessDaysCount)
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
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            DaysCount = leave.DaysCount,
            BusinessDaysCount = leave.BusinessDaysCount,
            Notes = leave.Notes,
            CreatedAt = leave.CreatedAt,
            UpdatedAt = leave.UpdatedAt
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
