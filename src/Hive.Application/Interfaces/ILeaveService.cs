using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for managing leave records.
/// </summary>
public interface ILeaveService
{
    Task<LeaveDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveDto>> GetUpcomingAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<LeaveDto> CreateAsync(CreateLeaveDto dto, CancellationToken cancellationToken = default);
    Task<CreatePublicHolidayResultDto> CreatePublicHolidayAsync(CreatePublicHolidayLeaveDto dto, CancellationToken cancellationToken = default);
    Task<LeaveDto> UpdateAsync(Guid id, UpdateLeaveDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamLeaveOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyLeaveSummaryDto>> GetMonthlyTrendAsync(int months = 12, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalanceDto>> GetTeamBalancesAsync(int year, CancellationToken cancellationToken = default);
}
