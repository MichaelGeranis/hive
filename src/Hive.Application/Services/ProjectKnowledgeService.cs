using System.Text.RegularExpressions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for ProjectKnowledge management.
/// </summary>
public partial class ProjectKnowledgeService : IProjectKnowledgeService
{
    private readonly IProjectKnowledgeRepository _knowledgeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;
    private readonly IActivityRepository _activityRepository;
    private readonly IKnowledgePointRepository _knowledgePointRepository;
    private readonly ITeamTaskRepository _teamTaskRepository;

    [GeneratedRegex(@"from level (\d) to level (\d)")]
    private static partial Regex LevelChangeRegex();

    [GeneratedRegex(@"(?:changed from|from) (\d+) (?:to|points to) (\d+)")]
    private static partial Regex PointsChangeRegex();

    [GeneratedRegex(@"Added (\d+) points")]
    private static partial Regex PointsAddedRegex();

    [GeneratedRegex(@"created with (\d+) manual points")]
    private static partial Regex PointsCreatedRegex();

    public ProjectKnowledgeService(
        IProjectKnowledgeRepository knowledgeRepository,
        IProjectRepository projectRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService,
        IActivityRepository activityRepository,
        IKnowledgePointRepository knowledgePointRepository,
        ITeamTaskRepository teamTaskRepository)
    {
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _knowledgePointRepository = knowledgePointRepository ?? throw new ArgumentNullException(nameof(knowledgePointRepository));
        _teamTaskRepository = teamTaskRepository ?? throw new ArgumentNullException(nameof(teamTaskRepository));
    }

    public async Task<ProjectKnowledgeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _knowledgeRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledgeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgeRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledgeDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgeRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectKnowledgeDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgeRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<ProjectKnowledgeMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var allScores = await _knowledgeRepository.GetAllAsync(cancellationToken);

        // Only include direct reports (not indirect reports)
        var directOnly = directReports.Where(dr => dr.IsDirect).ToList();

        var projectSummaries = projects.Select(p => new KnowledgeMatrixProjectDto
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        var directReportSummaries = directOnly.Select(dr => new KnowledgeMatrixMemberDto
        {
            Id = dr.Id,
            Name = dr.FullName
        }).ToList();

        var scoreDtos = allScores.Select(s =>
        {
            var project = projects.FirstOrDefault(p => p.Id == s.ProjectId);
            var directReport = directReports.FirstOrDefault(dr => dr.Id == s.DirectReportId);
            return new ProjectKnowledgeDto
            {
                Id = s.Id,
                DirectReportId = s.DirectReportId,
                DirectReportName = directReport?.FullName ?? "Unknown",
                ProjectId = s.ProjectId,
                ProjectName = project?.Name ?? "Unknown",
                KnowledgeLevel = s.KnowledgeLevel,
                KnowledgeLevelLabel = s.GetKnowledgeLevelLabel(),
                UpdatedAt = s.UpdatedAt
            };
        }).ToList();

        return new ProjectKnowledgeMatrixDto
        {
            Projects = projectSummaries,
            DirectReports = directReportSummaries,
            Scores = scoreDtos
        };
    }

    public async Task<ProjectKnowledgeDto> CreateOrUpdateAsync(CreateOrUpdateProjectKnowledgeDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(dto.DirectReportId, dto.ProjectId, cancellationToken);

        var existing = await _knowledgeRepository.GetByDirectReportAndProjectAsync(
            dto.DirectReportId, dto.ProjectId, cancellationToken);

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);

        if (existing is not null)
        {
            var previousLevel = existing.KnowledgeLevel;
            existing.Update(dto.KnowledgeLevel);
            await _knowledgeRepository.UpdateAsync(existing, cancellationToken);

            await _activityService.LogActivityAsync(
                ActivityType.Updated,
                EntityType.ProjectKnowledge,
                existing.Id,
                $"Knowledge: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
                $"Knowledge assessment changed from level {previousLevel} to level {dto.KnowledgeLevel}",
                cancellationToken);

            return await MapToDtoAsync(existing, cancellationToken);
        }

        var entity = new ProjectKnowledge(dto.DirectReportId, dto.ProjectId, dto.KnowledgeLevel);
        var created = await _knowledgeRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ProjectKnowledge,
            created.Id,
            $"Knowledge: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            $"Knowledge assessment created at level {dto.KnowledgeLevel}",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _knowledgeRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ProjectKnowledge), id);
        }

        var project = await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _knowledgeRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.ProjectKnowledge,
            id,
            $"Knowledge: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            $"Knowledge assessment was deleted",
            cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeProgressionEntryDto>> GetProgressionByDirectReportAsync(
        Guid directReportId,
        CancellationToken cancellationToken = default)
    {
        var progressionEntries = await GetAllProgressionEntriesAsync(cancellationToken);
        return progressionEntries
            .Where(e => e.DirectReportId == directReportId)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<KnowledgeProgressionEntryDto>> GetProgressionByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var progressionEntries = await GetAllProgressionEntriesAsync(cancellationToken);
        return progressionEntries
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private async Task<List<KnowledgeProgressionEntryDto>> GetAllProgressionEntriesAsync(
        CancellationToken cancellationToken)
    {
        // Get all ProjectKnowledge activities (Updated, Created, and Deleted types)
        var activities = await _activityRepository.GetByEntityTypeAsync(EntityType.ProjectKnowledge, cancellationToken);
        var updateActivities = activities
            .Where(a => a.ActivityType == ActivityType.Updated ||
                       a.ActivityType == ActivityType.Created ||
                       a.ActivityType == ActivityType.Deleted)
            .ToList();

        // Get all knowledge records, direct reports, and projects for lookups
        var knowledgeRecords = await _knowledgeRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var knowledgePoints = await _knowledgePointRepository.GetAllAsync(cancellationToken);
        var allTasks = await _teamTaskRepository.GetAllAsync(cancellationToken);

        var knowledgeLookup = knowledgeRecords.ToDictionary(k => k.Id);
        var drLookup = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectLookup = projects.ToDictionary(p => p.Id, p => p.Name);

        // Pre-calculate automatic points for all direct report + project combinations
        // to avoid expensive per-entry calculations
        var automaticPointsCache = new Dictionary<(Guid directReportId, Guid projectId), int>();
        var completedTasks = allTasks.Where(t => t.Status == Core.Entities.TaskStatus.Done).ToList();

        // Build project label lookups for label-based matching
        var projectLabelsLookup = projects.ToDictionary(
            p => p.Id,
            p => GetLabelSet(p.Labels)
        );

        foreach (var task in completedTasks)
        {
            if (!task.AssigneeId.HasValue) continue;

            var points = task.StoryPoints ?? 1;

            // Match task to projects (either by direct ProjectId or shared labels)
            foreach (var project in projects)
            {
                var isMatch = task.ProjectId == project.Id ||
                    (projectLabelsLookup.TryGetValue(project.Id, out var projectLabels) &&
                     projectLabels.Count > 0 &&
                     GetLabelSet(task.Labels).Overlaps(projectLabels));

                if (isMatch)
                {
                    var key = (task.AssigneeId.Value, project.Id);

                    if (automaticPointsCache.ContainsKey(key))
                    {
                        automaticPointsCache[key] += points;
                    }
                    else
                    {
                        automaticPointsCache[key] = points;
                    }
                }
            }
        }

        var result = new List<KnowledgeProgressionEntryDto>();

        // Build a lookup to resolve DirectReportId and ProjectId from KnowledgePoint entity IDs
        var knowledgePointsLookup = knowledgePoints.ToDictionary(kp => kp.Id);

        foreach (var activity in updateActivities)
        {
            // Try to parse as level change first
            var levelMatch = LevelChangeRegex().Match(activity.Description);
            var pointsMatch = PointsChangeRegex().Match(activity.Description);
            var pointsAddedMatch = PointsAddedRegex().Match(activity.Description);
            var pointsCreatedMatch = PointsCreatedRegex().Match(activity.Description);

            int? oldLevel = null;
            int? newLevel = null;
            int? change = null;
            int? manualPoints = null;
            int? automaticPoints = null;
            int? totalPoints = null;
            string entryType = "Unknown";
            Guid directReportId = Guid.Empty;
            Guid projectId = Guid.Empty;

            // Handle level changes
            if (levelMatch.Success)
            {
                if (int.TryParse(levelMatch.Groups[1].Value, out var oldLvl) &&
                    int.TryParse(levelMatch.Groups[2].Value, out var newLvl))
                {
                    oldLevel = oldLvl;
                    newLevel = newLvl;
                    change = newLvl - oldLvl;
                    entryType = "LevelChange";

                    // Get the knowledge record to find DirectReportId and ProjectId
                    if (knowledgeLookup.TryGetValue(activity.EntityId, out var knowledge))
                    {
                        directReportId = knowledge.DirectReportId;
                        projectId = knowledge.ProjectId;

                        // Get current points for this combination
                        var pointsRecord = knowledgePoints.FirstOrDefault(kp =>
                            kp.DirectReportId == directReportId &&
                            kp.ProjectId == projectId);

                        if (pointsRecord != null)
                        {
                            manualPoints = pointsRecord.ManualPoints;
                            var key = (directReportId, projectId);
                            automaticPoints = automaticPointsCache.GetValueOrDefault(key, 0);
                            totalPoints = manualPoints + automaticPoints;
                        }
                    }
                    else
                    {
                        continue; // Skip if we can't find the knowledge record
                    }
                }
            }
            // Handle points changes
            else if (pointsMatch.Success || pointsAddedMatch.Success || pointsCreatedMatch.Success)
            {
                entryType = "PointsChange";

                // Try to get DirectReportId and ProjectId from KnowledgePoint entity
                if (knowledgePointsLookup.TryGetValue(activity.EntityId, out var kpEntity))
                {
                    directReportId = kpEntity.DirectReportId;
                    projectId = kpEntity.ProjectId;

                    // Parse the points values from the description
                    if (pointsMatch.Success)
                    {
                        if (int.TryParse(pointsMatch.Groups[2].Value, out var newPts))
                        {
                            manualPoints = newPts;
                        }
                    }
                    else if (pointsAddedMatch.Success)
                    {
                        // For "Added X points", get current value
                        manualPoints = kpEntity.ManualPoints;
                    }
                    else if (pointsCreatedMatch.Success)
                    {
                        // For "created with X manual points", parse the value
                        if (int.TryParse(pointsCreatedMatch.Groups[1].Value, out var createdPts))
                        {
                            manualPoints = createdPts;
                        }
                    }

                    // Calculate automatic and total points
                    var key = (directReportId, projectId);
                    automaticPoints = automaticPointsCache.GetValueOrDefault(key, 0);
                    totalPoints = manualPoints + automaticPoints;
                }
                else
                {
                    continue; // Skip if we can't find the points record
                }
            }
            else
            {
                continue; // Skip if no pattern matches
            }

            // Add the progression entry
            if (directReportId != Guid.Empty && projectId != Guid.Empty)
            {
                result.Add(new KnowledgeProgressionEntryDto
                {
                    Id = activity.Id,
                    DirectReportId = directReportId,
                    DirectReportName = drLookup.GetValueOrDefault(directReportId, "Unknown"),
                    ProjectId = projectId,
                    ProjectName = projectLookup.GetValueOrDefault(projectId, "Unknown"),
                    OldLevel = oldLevel,
                    NewLevel = newLevel,
                    Change = change,
                    ManualPoints = manualPoints,
                    AutomaticPoints = automaticPoints,
                    TotalPoints = totalPoints,
                    EntryType = entryType,
                    Timestamp = activity.Timestamp
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Parses a comma-separated label string into a lowercase HashSet.
    /// </summary>
    private static HashSet<string> GetLabelSet(string? labels)
    {
        if (string.IsNullOrEmpty(labels))
        {
            return new HashSet<string>();
        }

        return labels
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().ToLowerInvariant())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToHashSet();
    }

    private async Task ValidateReferencesAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), directReportId);
        }

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException(nameof(Project), projectId);
        }
    }

    private async Task<ProjectKnowledgeDto> MapToDtoAsync(ProjectKnowledge entity, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        var project = await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken);

        return new ProjectKnowledgeDto
        {
            Id = entity.Id,
            DirectReportId = entity.DirectReportId,
            DirectReportName = directReport?.FullName ?? "Unknown",
            ProjectId = entity.ProjectId,
            ProjectName = project?.Name ?? "Unknown",
            KnowledgeLevel = entity.KnowledgeLevel,
            KnowledgeLevelLabel = entity.GetKnowledgeLevelLabel(),
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<ProjectKnowledgeDto>> MapToDtosAsync(IEnumerable<ProjectKnowledge> entities, CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);

        var drLookup = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectLookup = projects.ToDictionary(p => p.Id, p => p.Name);

        return entities.Select(e => new ProjectKnowledgeDto
        {
            Id = e.Id,
            DirectReportId = e.DirectReportId,
            DirectReportName = drLookup.GetValueOrDefault(e.DirectReportId, "Unknown"),
            ProjectId = e.ProjectId,
            ProjectName = projectLookup.GetValueOrDefault(e.ProjectId, "Unknown"),
            KnowledgeLevel = e.KnowledgeLevel,
            KnowledgeLevelLabel = e.GetKnowledgeLevelLabel(),
            UpdatedAt = e.UpdatedAt
        }).ToList();
    }
}
