using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for Skill entity.
/// </summary>
public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Skill>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Skill>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default);
    Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
