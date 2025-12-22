using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for Skill management.
/// </summary>
public interface ISkillService
{
    Task<SkillDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillDto>> GetByCategoryAsync(SkillCategory category, CancellationToken cancellationToken = default);
    Task<SkillDto> CreateAsync(CreateSkillDto dto, CancellationToken cancellationToken = default);
    Task<SkillDto> UpdateAsync(Guid id, UpdateSkillDto dto, CancellationToken cancellationToken = default);
    Task<SkillDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillDto> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
