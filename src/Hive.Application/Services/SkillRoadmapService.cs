using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Skill roadmaps and the built-in library they can be added from.
/// </summary>
public class SkillRoadmapService : ISkillRoadmapService
{
    private readonly ISkillRoadmapRepository _roadmapRepository;
    private readonly IRoadmapSkillRepository _skillRepository;
    private readonly IActivityService _activityService;

    public SkillRoadmapService(
        ISkillRoadmapRepository roadmapRepository,
        IRoadmapSkillRepository skillRepository,
        IActivityService activityService)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<IReadOnlyList<SkillRoadmapSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roadmaps = await _roadmapRepository.GetAllAsync(cancellationToken);
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        var skillsByRoadmap = skills.ToLookup(s => s.RoadmapId);

        return roadmaps.Select(r => MapToSummary(r, skillsByRoadmap[r.Id].ToList())).ToList();
    }

    public async Task<SkillRoadmapDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var roadmap = await _roadmapRepository.GetByIdAsync(id, cancellationToken);
        if (roadmap is null) return null;

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    public async Task<SkillRoadmapDto> CreateAsync(SaveSkillRoadmapDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsFreeAsync(dto.Name, null, cancellationToken);

        var roadmap = new SkillRoadmap(dto.Name, dto.Description);
        await _roadmapRepository.AddAsync(roadmap, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created, EntityType.SkillRoadmap, roadmap.Id, roadmap.Name,
            $"Roadmap '{roadmap.Name}' was created", cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    public async Task<SkillRoadmapDto> UpdateAsync(Guid id, SaveSkillRoadmapDto dto, CancellationToken cancellationToken = default)
    {
        var roadmap = await GetRoadmapAsync(id, cancellationToken);
        await EnsureNameIsFreeAsync(dto.Name, id, cancellationToken);

        roadmap.Update(dto.Name, dto.Description);
        await _roadmapRepository.UpdateAsync(roadmap, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated, EntityType.SkillRoadmap, roadmap.Id, roadmap.Name,
            $"Roadmap '{roadmap.Name}' was updated", cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    /// <summary>
    /// Deletes a roadmap and its skills. Career plans made from it keep their own copy of the skills.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var roadmap = await GetRoadmapAsync(id, cancellationToken);

        await _skillRepository.DeleteByRoadmapIdAsync(id, cancellationToken);
        await _roadmapRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted, EntityType.SkillRoadmap, roadmap.Id, roadmap.Name,
            $"Roadmap '{roadmap.Name}' was deleted", cancellationToken);
    }

    /// <summary>
    /// Adds a skill at the end of its section; a new section goes at the end of the roadmap.
    /// </summary>
    public async Task<SkillRoadmapDto> AddSkillAsync(Guid roadmapId, SaveRoadmapSkillDto dto, CancellationToken cancellationToken = default)
    {
        var roadmap = await GetRoadmapAsync(roadmapId, cancellationToken);
        var existing = await _skillRepository.GetByRoadmapIdAsync(roadmapId, cancellationToken);
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(s => s.SortOrder) + 1;

        var skill = new RoadmapSkill(
            roadmapId, dto.Section, dto.Title, ParseCategory(dto.Category), sortOrder, dto.Description, dto.IsOptional,
            CareerStageExpectations.Parse(dto.FromStage, nameof(dto.FromStage)));
        await _skillRepository.AddAsync(skill, cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    public async Task<SkillRoadmapDto> UpdateSkillAsync(Guid roadmapId, Guid skillId, SaveRoadmapSkillDto dto, CancellationToken cancellationToken = default)
    {
        var roadmap = await GetRoadmapAsync(roadmapId, cancellationToken);
        var skill = await GetSkillAsync(roadmapId, skillId, cancellationToken);

        skill.Update(dto.Section, dto.Title, ParseCategory(dto.Category), dto.Description, dto.IsOptional,
            CareerStageExpectations.Parse(dto.FromStage, nameof(dto.FromStage)));
        await _skillRepository.UpdateAsync(skill, cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    public async Task<SkillRoadmapDto> DeleteSkillAsync(Guid roadmapId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var roadmap = await GetRoadmapAsync(roadmapId, cancellationToken);
        await GetSkillAsync(roadmapId, skillId, cancellationToken);

        await _skillRepository.DeleteAsync(skillId, cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    public async Task<IReadOnlyList<RoadmapLibraryItemDto>> GetLibraryAsync(CancellationToken cancellationToken = default)
    {
        var roadmaps = await _roadmapRepository.GetAllAsync(cancellationToken);
        var addedKeys = roadmaps
            .Where(r => r.LibraryKey is not null)
            .Select(r => r.LibraryKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return RoadmapLibrary.All.Select(r => new RoadmapLibraryItemDto
        {
            Key = r.Key,
            Name = r.Name,
            Description = r.Description,
            SectionCount = r.Sections.Count,
            SkillCount = r.Skills.Count(),
            HardSkillCount = r.HardSkillCount,
            SoftSkillCount = r.SoftSkillCount,
            IsAdded = addedKeys.Contains(r.Key)
        }).ToList();
    }

    public async Task<SkillRoadmapDto> AddFromLibraryAsync(string key, CancellationToken cancellationToken = default)
    {
        var source = RoadmapLibrary.Find(key)
            ?? throw new NotFoundException("Library roadmap", key);

        if (await _roadmapRepository.NameExistsAsync(source.Name, null, cancellationToken))
        {
            throw new ConflictException($"A roadmap named '{source.Name}' is already in Hive. Rename it to add the built-in one again.");
        }

        var (roadmap, skills) = RoadmapLibrary.Instantiate(source);
        await _roadmapRepository.AddAsync(roadmap, cancellationToken);
        await _skillRepository.AddRangeAsync(skills, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created, EntityType.SkillRoadmap, roadmap.Id, roadmap.Name,
            $"Roadmap '{roadmap.Name}' was added from the library with {skills.Count} skills", cancellationToken);

        return await MapToDtoAsync(roadmap, cancellationToken);
    }

    /// <summary>
    /// Reads a skill category sent as <c>Hard</c> or <c>Soft</c>, ignoring case.
    /// </summary>
    internal static SkillCategory ParseCategory(string? category)
    {
        if (!string.IsNullOrWhiteSpace(category)
            && Enum.TryParse<SkillCategory>(category.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ArgumentException("Skill category must be Hard or Soft.", nameof(category));
    }

    private async Task EnsureNameIsFreeAsync(string? name, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(name) && await _roadmapRepository.NameExistsAsync(name, excludeId, cancellationToken))
        {
            throw new ConflictException($"A roadmap named '{name.Trim()}' already exists.");
        }
    }

    private async Task<SkillRoadmap> GetRoadmapAsync(Guid id, CancellationToken cancellationToken) =>
        await _roadmapRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SkillRoadmap), id);

    private async Task<RoadmapSkill> GetSkillAsync(Guid roadmapId, Guid skillId, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(skillId, cancellationToken);
        if (skill is null || skill.RoadmapId != roadmapId)
        {
            throw new NotFoundException(nameof(RoadmapSkill), skillId);
        }

        return skill;
    }

    private async Task<SkillRoadmapDto> MapToDtoAsync(SkillRoadmap roadmap, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetByRoadmapIdAsync(roadmap.Id, cancellationToken);
        var summary = MapToSummary(roadmap, skills);

        return new SkillRoadmapDto
        {
            Id = summary.Id,
            Name = summary.Name,
            Description = summary.Description,
            LibraryKey = summary.LibraryKey,
            SectionCount = summary.SectionCount,
            SkillCount = summary.SkillCount,
            HardSkillCount = summary.HardSkillCount,
            SoftSkillCount = summary.SoftSkillCount,
            SkillCountsByStage = summary.SkillCountsByStage,
            CreatedAt = summary.CreatedAt,
            Sections = skills
                .GroupBy(s => s.Section, StringComparer.OrdinalIgnoreCase)
                .Select(g => new RoadmapSectionDto
                {
                    Name = g.First().Section,
                    Skills = g.Select(s => new RoadmapSkillDto
                    {
                        Id = s.Id,
                        Section = s.Section,
                        Title = s.Title,
                        Description = s.Description,
                        Category = s.Category.ToString(),
                        IsOptional = s.IsOptional,
                        FromStage = s.FromStage.ToString(),
                        SortOrder = s.SortOrder
                    }).ToList()
                })
                .ToList()
        };
    }

    private static SkillRoadmapSummaryDto MapToSummary(SkillRoadmap roadmap, IReadOnlyList<RoadmapSkill> skills) => new()
    {
        Id = roadmap.Id,
        Name = roadmap.Name,
        Description = roadmap.Description,
        LibraryKey = roadmap.LibraryKey,
        SectionCount = skills.Select(s => s.Section).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
        SkillCount = skills.Count,
        HardSkillCount = skills.Count(s => s.Category == SkillCategory.Hard),
        SoftSkillCount = skills.Count(s => s.Category == SkillCategory.Soft),
        SkillCountsByStage = Enum.GetValues<CareerStage>()
            .ToDictionary(stage => stage.ToString(), stage => skills.Count(s => s.AppliesTo(stage))),
        CreatedAt = roadmap.CreatedAt
    };
}
