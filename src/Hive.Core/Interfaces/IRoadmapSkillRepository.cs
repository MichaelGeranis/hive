using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for RoadmapSkill entities.
/// </summary>
public interface IRoadmapSkillRepository
{
    Task<RoadmapSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// A roadmap's skills, by sort order, then title.
    /// </summary>
    Task<IReadOnlyList<RoadmapSkill>> GetByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every roadmap's skills, by sort order, then title.
    /// </summary>
    Task<IReadOnlyList<RoadmapSkill>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RoadmapSkill> AddAsync(RoadmapSkill skill, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<RoadmapSkill> skills, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoadmapSkill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByRoadmapIdAsync(Guid roadmapId, CancellationToken cancellationToken = default);
}
