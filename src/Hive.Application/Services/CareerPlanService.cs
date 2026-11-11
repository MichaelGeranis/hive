using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Career plans: one per direct report, made from a roadmap. Rating skills and choosing what to
/// work on; the ladder reading (<see cref="CareerLadder"/>) and the game
/// (<see cref="CareerProgress"/>) are derived on every read.
/// </summary>
public class CareerPlanService : ICareerPlanService
{
    private readonly ICareerPlanRepository _planRepository;
    private readonly ICareerPlanSkillRepository _skillRepository;
    private readonly ISkillRoadmapRepository _roadmapRepository;
    private readonly IRoadmapSkillRepository _roadmapSkillRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;
    private readonly TimeProvider _timeProvider;

    public CareerPlanService(
        ICareerPlanRepository planRepository,
        ICareerPlanSkillRepository skillRepository,
        ISkillRoadmapRepository roadmapRepository,
        IRoadmapSkillRepository roadmapSkillRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService,
        TimeProvider? timeProvider = null)
    {
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _roadmapSkillRepository = roadmapSkillRepository ?? throw new ArgumentNullException(nameof(roadmapSkillRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<CareerPlanSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _planRepository.GetAllAsync(cancellationToken);
        var names = await GetNamesAsync(cancellationToken);
        var skills = (await _skillRepository.GetAllAsync(cancellationToken)).ToLookup(s => s.CareerPlanId);
        var now = Now;

        return plans
            .Select(plan => MapSummary<CareerPlanSummaryDto>(plan, names.GetValueOrDefault(plan.DirectReportId, "Unknown"), skills[plan.Id].ToList(), now))
            .OrderBy(p => p.DirectReportName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<CareerPlanDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        return plan is null ? null : await MapToDtoAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanDto?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return plan is null ? null : await MapToDtoAsync(plan, cancellationToken);
    }

    public async Task<CareerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _planRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var names = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var skills = (await _skillRepository.GetAllAsync(cancellationToken)).ToLookup(s => s.CareerPlanId);
        var withPlan = plans.Select(p => p.DirectReportId).ToHashSet();
        var now = Now;

        CareerAttentionDto Attention(CareerPlan plan) => new()
        {
            DirectReportId = plan.DirectReportId,
            FullName = names.GetValueOrDefault(plan.DirectReportId, "Unknown"),
            PlanId = plan.Id,
            LastChangedAt = LastChangedAt(plan, skills[plan.Id].ToList())
        };

        return new CareerOverviewDto
        {
            DirectReportsWithoutPlan = directReports
                .Where(dr => dr.IsDirect && !withPlan.Contains(dr.Id))
                .OrderBy(dr => dr.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(dr => new CareerAttentionDto { DirectReportId = dr.Id, FullName = dr.FullName })
                .ToList(),
            NothingInFocus = plans
                .Where(p => !skills[p.Id].Any(s => s.IsFocus))
                .Select(Attention)
                .OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Quiet = plans
                .Where(p => p.IsQuiet(skills[p.Id].Max(s => s.UpdatedAt), now))
                .Select(Attention)
                .OrderBy(a => a.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    /// <summary>
    /// Makes a direct report's plan by copying every skill of the roadmap, not started. A direct
    /// report has one plan.
    /// </summary>
    public async Task<CareerPlanDto> CreateAsync(CreateCareerPlanDto dto, CancellationToken cancellationToken = default)
    {
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        var roadmap = await _roadmapRepository.GetByIdAsync(dto.RoadmapId, cancellationToken)
            ?? throw new NotFoundException(nameof(SkillRoadmap), dto.RoadmapId);

        if (await _planRepository.ExistsForDirectReportAsync(directReport.Id, cancellationToken))
        {
            throw new ConflictException($"{directReport.FullName} already has a career plan.");
        }

        var stage = CareerStageExpectations.Parse(dto.CurrentStage, nameof(dto.CurrentStage));
        var plan = new CareerPlan(directReport.Id, roadmap.Name, stage, dto.Notes);
        var roadmapSkills = await _roadmapSkillRepository.GetByRoadmapIdAsync(roadmap.Id, cancellationToken);

        await _planRepository.AddAsync(plan, cancellationToken);
        await _skillRepository.AddRangeAsync(roadmapSkills.Select(s => CareerPlanSkill.FromRoadmap(plan.Id, s)).ToList(), cancellationToken);

        await LogAsync(ActivityType.Created, plan, directReport.FullName,
            $"Career plan started from '{roadmap.Name}', as {stage}", cancellationToken);

        return await MapToDtoAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanDto> UpdateAsync(Guid id, UpdateCareerPlanDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(id, cancellationToken);
        var previousStage = plan.CurrentStage;

        plan.Update(CareerStageExpectations.Parse(dto.CurrentStage, nameof(dto.CurrentStage)), dto.Notes);
        await _planRepository.UpdateAsync(plan, cancellationToken);

        var description = plan.CurrentStage != previousStage
            ? $"Role stage changed from {previousStage} to {plan.CurrentStage}"
            : "Career plan notes were updated";
        await LogAsync(ActivityType.Updated, plan, null, description, cancellationToken);

        return await MapToDtoAsync(plan, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(id, cancellationToken);

        await _skillRepository.DeleteByCareerPlanIdAsync(id, cancellationToken);
        await _planRepository.DeleteAsync(id, cancellationToken);

        await LogAsync(ActivityType.Deleted, plan, null, "Career plan was deleted", cancellationToken);
    }

    /// <summary>
    /// Rates a skill, and reports what the rating unlocked: a skill level-up, a player level-up
    /// and any newly earned badges.
    /// </summary>
    public async Task<SkillLevelChangeDto> SetSkillLevelAsync(Guid planId, Guid skillId, SetSkillLevelDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(planId, cancellationToken);
        var skills = await _skillRepository.GetByCareerPlanIdAsync(planId, cancellationToken);
        var skill = skills.FirstOrDefault(s => s.Id == skillId)
            ?? throw new NotFoundException(nameof(CareerPlanSkill), skillId);

        var before = Game(plan, skills);
        var previousLevel = skill.Level;

        var levelledUp = skill.SetLevel(dto.Level, dto.Evidence);
        await _skillRepository.UpdateAsync(skill, cancellationToken);

        var after = Game(plan, skills);
        var earnedBefore = before.Badges.Where(b => b.IsEarned).Select(b => b.Key).ToHashSet();

        if (skill.Level != previousLevel)
        {
            var description = levelledUp
                ? $"Levelled up {skill.Title} to {SkillLevel.GetLabel(skill.Level)} ({skill.Level}/{SkillLevel.Max})"
                : $"{skill.Title} rated {SkillLevel.GetLabel(skill.Level)} ({skill.Level}/{SkillLevel.Max})";
            await LogAsync(ActivityType.Updated, plan, null, description, cancellationToken);
        }

        return new SkillLevelChangeDto
        {
            Plan = await MapToDtoAsync(plan, cancellationToken),
            SkillLevelledUp = levelledUp,
            PlayerLevelledUp = after.PlayerLevel > before.PlayerLevel,
            NewBadges = after.Badges
                .Where(b => b.IsEarned && !earnedBefore.Contains(b.Key))
                .Select(MapBadge)
                .ToList()
        };
    }

    public async Task<CareerPlanDto> SetSkillFocusAsync(Guid planId, Guid skillId, SetSkillFocusDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(planId, cancellationToken);
        var skill = await GetSkillAsync(planId, skillId, cancellationToken);

        if (skill.IsFocus != dto.IsFocus)
        {
            skill.SetFocus(dto.IsFocus);
            await _skillRepository.UpdateAsync(skill, cancellationToken);

            await LogAsync(ActivityType.Updated, plan, null,
                dto.IsFocus ? $"Started working on {skill.Title}" : $"Stopped working on {skill.Title}", cancellationToken);
        }

        return await MapToDtoAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Adds a skill to this plan only, at the end of its section.
    /// </summary>
    public async Task<CareerPlanDto> AddSkillAsync(Guid planId, AddCareerPlanSkillDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(planId, cancellationToken);
        var existing = await _skillRepository.GetByCareerPlanIdAsync(planId, cancellationToken);
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(s => s.SortOrder) + 1;

        var skill = new CareerPlanSkill(
            planId, dto.Section, dto.Title, SkillRoadmapService.ParseCategory(dto.Category), sortOrder,
            dto.Description, dto.IsOptional, CareerStageExpectations.Parse(dto.FromStage, nameof(dto.FromStage)));
        await _skillRepository.AddAsync(skill, cancellationToken);

        return await MapToDtoAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanDto> DeleteSkillAsync(Guid planId, Guid skillId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(planId, cancellationToken);
        await GetSkillAsync(planId, skillId, cancellationToken);

        await _skillRepository.DeleteAsync(skillId, cancellationToken);

        return await MapToDtoAsync(plan, cancellationToken);
    }

    private CareerProgress Game(CareerPlan plan, IReadOnlyList<CareerPlanSkill> skills) =>
        CareerProgress.Calculate(skills, CareerLadder.Assess(skills, plan.CurrentStage), Now);

    private static DateTime LastChangedAt(CareerPlan plan, IReadOnlyList<CareerPlanSkill> skills) =>
        new[] { plan.CreatedAt, plan.UpdatedAt ?? plan.CreatedAt, skills.Max(s => s.UpdatedAt) ?? plan.CreatedAt }.Max();

    private async Task<Dictionary<Guid, string>> GetNamesAsync(CancellationToken cancellationToken) =>
        (await _directReportRepository.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id, d => d.FullName);

    private async Task<CareerPlan> GetPlanAsync(Guid id, CancellationToken cancellationToken) =>
        await _planRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CareerPlan), id);

    private async Task<CareerPlanSkill> GetSkillAsync(Guid planId, Guid skillId, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(skillId, cancellationToken);
        if (skill is null || skill.CareerPlanId != planId)
        {
            throw new NotFoundException(nameof(CareerPlanSkill), skillId);
        }

        return skill;
    }

    private async Task LogAsync(ActivityType type, CareerPlan plan, string? directReportName, string description, CancellationToken cancellationToken)
    {
        directReportName ??= (await _directReportRepository.GetByIdAsync(plan.DirectReportId, cancellationToken))?.FullName ?? "Unknown";
        await _activityService.LogActivityAsync(
            type, EntityType.CareerPlan, plan.Id, $"{directReportName}'s career plan", description, cancellationToken);
    }

    private async Task<CareerPlanDto> MapToDtoAsync(CareerPlan plan, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(plan.DirectReportId, cancellationToken);
        var skills = await _skillRepository.GetByCareerPlanIdAsync(plan.Id, cancellationToken);
        var now = Now;
        var ladder = CareerLadder.Assess(skills, plan.CurrentStage);
        var game = CareerProgress.Calculate(skills, ladder, now);
        var summary = MapSummary<CareerPlanDto>(plan, directReport?.FullName ?? "Unknown", skills, now);
        var sectionsByName = game.Sections.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        CareerPlanSkillDto Skill(CareerPlanSkill s) => MapSkill(s, ladder);

        return summary with
        {
            Notes = plan.Notes,
            SkillCount = game.SkillCount,
            HasCount = game.HasCount,
            NotYetCount = game.NotYetCount,
            Sections = skills
                .GroupBy(s => s.Section, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var section = sectionsByName[g.Key];
                    return new CareerPlanSectionDto
                    {
                        Name = section.Name,
                        SkillCount = section.SkillCount,
                        HasCount = section.HasCount,
                        WorkingOnCount = section.WorkingOnCount,
                        IsCleared = section.IsCleared,
                        Skills = g.Select(Skill).ToList()
                    };
                })
                .ToList(),
            Ladder = new CareerLadderDto
            {
                CurrentStage = ladder.CurrentStage.ToString(),
                SkillsStage = ladder.SkillsStage?.ToString(),
                Reading = ladder.Reading.ToString(),
                Stages = ladder.Stages.Select(s => new StageFitDto
                {
                    Stage = s.Stage.ToString(),
                    ExpectedCount = s.ExpectedCount,
                    MetCount = s.MetCount,
                    MetPercent = s.MetPercent
                }).ToList(),
                Strengths = ladder.Strengths.Select(Skill).ToList(),
                Weaknesses = ladder.Weaknesses.Select(Skill).ToList()
            },
            Game = new CareerGameDto
            {
                Xp = game.Xp,
                PlayerLevel = game.PlayerLevel,
                PlayerTitle = game.PlayerTitle,
                CurrentLevelXp = game.CurrentLevelXp,
                NextLevelXp = game.NextLevelXp,
                SkillsLevelledUpRecently = game.SkillsLevelledUpRecently,
                Badges = game.Badges.Select(MapBadge).ToList()
            }
        };
    }

    private static T MapSummary<T>(CareerPlan plan, string directReportName, IReadOnlyList<CareerPlanSkill> skills, DateTime now)
        where T : CareerPlanSummaryDto, new() => new()
        {
            Id = plan.Id,
            DirectReportId = plan.DirectReportId,
            DirectReportName = directReportName,
            RoadmapName = plan.RoadmapName,
            CurrentStage = plan.CurrentStage.ToString(),
            WorkingOnCount = skills.Count(s => s.IsFocus),
            CreatedAt = plan.CreatedAt,
            LastChangedAt = LastChangedAt(plan, skills),
            IsQuiet = plan.IsQuiet(skills.Max(s => s.UpdatedAt), now)
        };

    private static CareerPlanSkillDto MapSkill(CareerPlanSkill skill, CareerLadder ladder) => new()
    {
        Id = skill.Id,
        Section = skill.Section,
        Title = skill.Title,
        Description = skill.Description,
        Category = skill.Category.ToString(),
        IsOptional = skill.IsOptional,
        FromStage = skill.FromStage.ToString(),
        Level = skill.Level,
        LevelLabel = SkillLevel.GetLabel(skill.Level),
        Status = skill.Status.ToString(),
        IsFocus = skill.IsFocus,
        ExpectedLevel = ladder.ExpectedLevel(skill),
        Evidence = skill.Evidence,
        LastLevelUpAt = skill.LastLevelUpAt,
        UpdatedAt = skill.UpdatedAt
    };

    private static CareerBadgeDto MapBadge(CareerBadge badge) => new()
    {
        Key = badge.Key,
        Name = badge.Name,
        Description = badge.Description,
        IsEarned = badge.IsEarned
    };
}
