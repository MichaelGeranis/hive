using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for SkillAssessment management.
/// </summary>
public class SkillAssessmentService : ISkillAssessmentService
{
    private readonly ISkillAssessmentRepository _assessmentRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    public SkillAssessmentService(
        ISkillAssessmentRepository assessmentRepository,
        ISkillRepository skillRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _assessmentRepository = assessmentRepository ?? throw new ArgumentNullException(nameof(assessmentRepository));
        _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<SkillAssessmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _assessmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _assessmentRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessmentDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _assessmentRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessmentDto>> GetBySkillIdAsync(Guid skillId, CancellationToken cancellationToken = default)
    {
        var entities = await _assessmentRepository.GetBySkillIdAsync(skillId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<SkillAssessmentDto> CreateAsync(CreateSkillAssessmentDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(dto.DirectReportId, dto.SkillId, cancellationToken);

        if (await _assessmentRepository.AssessmentExistsAsync(dto.DirectReportId, dto.SkillId, cancellationToken: cancellationToken))
        {
            throw new ConflictException("An assessment for this skill and direct report already exists.");
        }

        var entity = new SkillAssessment(dto.DirectReportId, dto.SkillId, dto.Level, dto.TargetLevel, dto.Notes);
        var created = await _assessmentRepository.AddAsync(entity, cancellationToken);

        var skill = await _skillRepository.GetByIdAsync(dto.SkillId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.SkillAssessment,
            created.Id,
            $"Assessment for '{skill?.Name ?? "Unknown"}' - {directReport?.FullName ?? "Unknown"}",
            $"Skill assessment was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<SkillAssessmentDto> UpdateAsync(Guid id, UpdateSkillAssessmentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _assessmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillAssessment), id);
        }

        entity.UpdateAssessment(dto.Level, dto.TargetLevel, dto.Notes);
        await _assessmentRepository.UpdateAsync(entity, cancellationToken);

        var skill = await _skillRepository.GetByIdAsync(entity.SkillId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.SkillAssessment,
            entity.Id,
            $"Assessment for '{skill?.Name ?? "Unknown"}' - {directReport?.FullName ?? "Unknown"}",
            $"Skill assessment was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<SkillAssessmentDto>> BulkAssessAsync(BulkSkillAssessmentDto dto, CancellationToken cancellationToken = default)
    {
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        }

        var results = new List<SkillAssessment>();

        foreach (var assessment in dto.Assessments)
        {
            var skill = await _skillRepository.GetByIdAsync(assessment.SkillId, cancellationToken);
            if (skill is null)
            {
                throw new NotFoundException(nameof(Skill), assessment.SkillId);
            }

            var existing = await _assessmentRepository.GetByDirectReportAndSkillAsync(dto.DirectReportId, assessment.SkillId, cancellationToken);

            if (existing is not null)
            {
                existing.UpdateAssessment(assessment.Level, assessment.TargetLevel, assessment.Notes);
                await _assessmentRepository.UpdateAsync(existing, cancellationToken);
                results.Add(existing);
            }
            else
            {
                var entity = new SkillAssessment(dto.DirectReportId, assessment.SkillId, assessment.Level, assessment.TargetLevel, assessment.Notes);
                var created = await _assessmentRepository.AddAsync(entity, cancellationToken);
                results.Add(created);
            }
        }

        return await MapToDtosAsync(results, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _assessmentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SkillAssessment), id);
        }

        var skill = await _skillRepository.GetByIdAsync(entity.SkillId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _assessmentRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.SkillAssessment,
            id,
            $"Assessment for '{skill?.Name ?? "Unknown"}' - {directReport?.FullName ?? "Unknown"}",
            $"Skill assessment was deleted",
            cancellationToken);
    }

    public async Task<SkillMatrixDto> GetSkillMatrixAsync(CancellationToken cancellationToken = default)
    {
        var skills = await _skillRepository.GetAllAsync(false, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var allAssessments = await _assessmentRepository.GetAllAsync(cancellationToken);

        var skillDtos = skills.Select(s => new SkillDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Category = s.Category,
            CategoryName = GetCategoryName(s.Category),
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();

        var directReportSkills = new List<DirectReportSkillsDto>();

        foreach (var dr in directReports)
        {
            var drAssessments = allAssessments
                .Where(a => a.DirectReportId == dr.Id)
                .Select(a =>
                {
                    var skill = skills.FirstOrDefault(s => s.Id == a.SkillId);
                    return new SkillAssessmentDto
                    {
                        Id = a.Id,
                        DirectReportId = a.DirectReportId,
                        DirectReportName = dr.FullName,
                        SkillId = a.SkillId,
                        SkillName = skill?.Name ?? "Unknown",
                        SkillCategory = skill?.Category ?? SkillCategory.Technical,
                        Level = a.Level,
                        LevelName = GetLevelName(a.Level),
                        TargetLevel = a.TargetLevel,
                        TargetLevelName = a.TargetLevel.HasValue ? GetLevelName(a.TargetLevel.Value) : null,
                        SkillGap = a.GetSkillGap(),
                        MeetsTarget = a.MeetsTarget(),
                        Notes = a.Notes,
                        UpdatedAt = a.UpdatedAt
                    };
                })
                .ToList();

            directReportSkills.Add(new DirectReportSkillsDto
            {
                DirectReportId = dr.Id,
                DirectReportName = dr.FullName,
                Assessments = drAssessments
            });
        }

        return new SkillMatrixDto
        {
            Skills = skillDtos,
            DirectReports = directReportSkills
        };
    }

    public async Task<IReadOnlyList<SkillAssessmentDto>> GetSkillGapsAsync(CancellationToken cancellationToken = default)
    {
        var allAssessments = await _assessmentRepository.GetAllAsync(cancellationToken);
        var gaps = allAssessments.Where(a => !a.MeetsTarget()).ToList();
        return await MapToDtosAsync(gaps, cancellationToken);
    }

    private async Task ValidateReferencesAsync(Guid directReportId, Guid skillId, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), directReportId);
        }

        var skill = await _skillRepository.GetByIdAsync(skillId, cancellationToken);
        if (skill is null)
        {
            throw new NotFoundException(nameof(Skill), skillId);
        }

        if (!skill.IsActive)
        {
            throw new InvalidOperationException("Cannot assess an inactive skill.");
        }
    }

    private async Task<SkillAssessmentDto> MapToDtoAsync(SkillAssessment entity, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        var skill = await _skillRepository.GetByIdAsync(entity.SkillId, cancellationToken);

        return new SkillAssessmentDto
        {
            Id = entity.Id,
            DirectReportId = entity.DirectReportId,
            DirectReportName = directReport?.FullName ?? "Unknown",
            SkillId = entity.SkillId,
            SkillName = skill?.Name ?? "Unknown",
            SkillCategory = skill?.Category ?? SkillCategory.Technical,
            Level = entity.Level,
            LevelName = GetLevelName(entity.Level),
            TargetLevel = entity.TargetLevel,
            TargetLevelName = entity.TargetLevel.HasValue ? GetLevelName(entity.TargetLevel.Value) : null,
            SkillGap = entity.GetSkillGap(),
            MeetsTarget = entity.MeetsTarget(),
            Notes = entity.Notes,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<SkillAssessmentDto>> MapToDtosAsync(IEnumerable<SkillAssessment> entities, CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var skills = await _skillRepository.GetAllAsync(true, cancellationToken);

        var drLookup = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var skillLookup = skills.ToDictionary(s => s.Id, s => (s.Name, s.Category));

        return entities.Select(e =>
        {
            var (skillName, skillCategory) = skillLookup.GetValueOrDefault(e.SkillId, ("Unknown", SkillCategory.Technical));
            return new SkillAssessmentDto
            {
                Id = e.Id,
                DirectReportId = e.DirectReportId,
                DirectReportName = drLookup.GetValueOrDefault(e.DirectReportId, "Unknown"),
                SkillId = e.SkillId,
                SkillName = skillName,
                SkillCategory = skillCategory,
                Level = e.Level,
                LevelName = GetLevelName(e.Level),
                TargetLevel = e.TargetLevel,
                TargetLevelName = e.TargetLevel.HasValue ? GetLevelName(e.TargetLevel.Value) : null,
                SkillGap = e.GetSkillGap(),
                MeetsTarget = e.MeetsTarget(),
                Notes = e.Notes,
                UpdatedAt = e.UpdatedAt
            };
        }).ToList();
    }

    private static string GetLevelName(ProficiencyLevel level) => level switch
    {
        ProficiencyLevel.None => "None",
        ProficiencyLevel.Novice => "Novice",
        ProficiencyLevel.Beginner => "Beginner",
        ProficiencyLevel.Intermediate => "Intermediate",
        ProficiencyLevel.Advanced => "Advanced",
        ProficiencyLevel.Expert => "Expert",
        _ => "Unknown"
    };

    private static string GetCategoryName(SkillCategory category) => category switch
    {
        SkillCategory.Technical => "Technical",
        SkillCategory.SoftSkills => "Soft Skills",
        SkillCategory.Leadership => "Leadership",
        SkillCategory.DomainKnowledge => "Domain Knowledge",
        SkillCategory.Tools => "Tools",
        _ => "Unknown"
    };
}
