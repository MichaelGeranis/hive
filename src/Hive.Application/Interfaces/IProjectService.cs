using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for Project management.
/// </summary>
public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
