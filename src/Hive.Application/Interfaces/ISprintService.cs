using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for Sprint management.
/// </summary>
public interface ISprintService
{
    Task<SprintDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SprintDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintDto>> GetByTeamAsync(string teamName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintDto>> GetByYearQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprintDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
    Task<SprintDto> CreateAsync(CreateSprintDto dto, CancellationToken cancellationToken = default);
    Task<SprintDto> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
