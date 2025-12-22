using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for OneOnOneMeeting management.
/// </summary>
public interface IOneOnOneMeetingService
{
    Task<OneOnOneMeetingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetByStatusAsync(MeetingStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto?> GetNextMeetingAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> CreateAsync(CreateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> UpdateAsync(Guid id, UpdateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> RescheduleAsync(Guid id, RescheduleMeetingDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
