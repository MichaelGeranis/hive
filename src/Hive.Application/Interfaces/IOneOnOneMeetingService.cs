using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for OneOnOneMeeting management.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
public interface IOneOnOneMeetingService
{
    Task<OneOnOneMeetingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> CreateAsync(CreateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> UpdateAsync(Guid id, UpdateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
