using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for SkillRoadmap entities.
/// </summary>
public interface ISkillRoadmapRepository
{
    Task<SkillRoadmap?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every roadmap, by name.
    /// </summary>
    Task<IReadOnlyList<SkillRoadmap>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SkillRoadmap> AddAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default);
    Task UpdateAsync(SkillRoadmap roadmap, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether another roadmap already has this name, ignoring case.
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
