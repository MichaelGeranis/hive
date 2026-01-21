using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for SkillCategory management.
/// </summary>
public interface ISkillCategoryService
{
    Task<SkillCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillCategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<SkillCategoryDto> CreateAsync(CreateSkillCategoryDto dto, CancellationToken cancellationToken = default);
    Task<SkillCategoryDto> UpdateAsync(Guid id, UpdateSkillCategoryDto dto, CancellationToken cancellationToken = default);
    Task<SkillCategoryDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillCategoryDto> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
