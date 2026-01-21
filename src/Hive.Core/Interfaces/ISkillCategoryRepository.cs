using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for SkillCategoryEntity.
/// </summary>
public interface ISkillCategoryRepository
{
    Task<SkillCategoryEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillCategoryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillCategoryEntity>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<SkillCategoryEntity> AddAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default);
    Task UpdateAsync(SkillCategoryEntity category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasSkillsAsync(Guid id, CancellationToken cancellationToken = default);
}
