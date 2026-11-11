using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Skill roadmaps: the reusable skill lists career plans are made from, and the built-in library.
/// </summary>
public interface ISkillRoadmapService
{
    Task<IReadOnlyList<SkillRoadmapSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SkillRoadmapDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkillRoadmapDto> CreateAsync(SaveSkillRoadmapDto dto, CancellationToken cancellationToken = default);
    Task<SkillRoadmapDto> UpdateAsync(Guid id, SaveSkillRoadmapDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SkillRoadmapDto> AddSkillAsync(Guid roadmapId, SaveRoadmapSkillDto dto, CancellationToken cancellationToken = default);
    Task<SkillRoadmapDto> UpdateSkillAsync(Guid roadmapId, Guid skillId, SaveRoadmapSkillDto dto, CancellationToken cancellationToken = default);
    Task<SkillRoadmapDto> DeleteSkillAsync(Guid roadmapId, Guid skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The built-in roadmaps, and whether each is already in Hive.
    /// </summary>
    Task<IReadOnlyList<RoadmapLibraryItemDto>> GetLibraryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a built-in roadmap into Hive as an ordinary, editable roadmap.
    /// </summary>
    Task<SkillRoadmapDto> AddFromLibraryAsync(string key, CancellationToken cancellationToken = default);
}
