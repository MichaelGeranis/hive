using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for SprintCapacity management.
/// </summary>
public interface ISprintCapacityService
{
    Task<SprintCapacityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SprintCapacityDto?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintCapacityDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SprintCapacityDto> CreateOrUpdateAsync(CreateSprintCapacityDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
