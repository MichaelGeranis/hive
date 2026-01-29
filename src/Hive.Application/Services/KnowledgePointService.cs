using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for KnowledgePoint management.
/// </summary>
public class KnowledgePointService : IKnowledgePointService
{
    private readonly IKnowledgePointRepository _knowledgePointRepository;
    private readonly IProjectKnowledgeRepository _projectKnowledgeRepository;
    private readonly ITeamTaskRepository _teamTaskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    // Level thresholds: points needed to suggest upgrade to each level
    // Level 1 is default (0-4 points), Level 2 at 5 points, etc.
    private static readonly Dictionary<int, int> LevelThresholds = new()
    {
        { 2, 5 },   // 5 points to suggest level 2
        { 3, 13 },  // 13 points to suggest level 3
        { 4, 21 },  // 21 points to suggest level 4
        { 5, 55 }   // 55 points to suggest level 5
    };

    public KnowledgePointService(
        IKnowledgePointRepository knowledgePointRepository,
        IProjectKnowledgeRepository projectKnowledgeRepository,
        ITeamTaskRepository teamTaskRepository,
        IProjectRepository projectRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _knowledgePointRepository = knowledgePointRepository ?? throw new ArgumentNullException(nameof(knowledgePointRepository));
        _projectKnowledgeRepository = projectKnowledgeRepository ?? throw new ArgumentNullException(nameof(projectKnowledgeRepository));
        _teamTaskRepository = teamTaskRepository ?? throw new ArgumentNullException(nameof(teamTaskRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<KnowledgePointDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _knowledgePointRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<KnowledgePointDto?> GetByDirectReportAndProjectAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var entity = await _knowledgePointRepository.GetByDirectReportAndProjectAsync(directReportId, projectId, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePointDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgePointRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePointDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgePointRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgePointDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = await _knowledgePointRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<KnowledgePointDto> CreateOrUpdateAsync(CreateOrUpdateKnowledgePointDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(dto.DirectReportId, dto.ProjectId, cancellationToken);

        var existing = await _knowledgePointRepository.GetByDirectReportAndProjectAsync(
            dto.DirectReportId, dto.ProjectId, cancellationToken);

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);

        if (existing is not null)
        {
            var previousPoints = existing.ManualPoints;
            existing.UpdateManualPoints(dto.ManualPoints, dto.Notes);
            await _knowledgePointRepository.UpdateAsync(existing, cancellationToken);

            await _activityService.LogActivityAsync(
                ActivityType.Updated,
                EntityType.ProjectKnowledge,
                existing.Id,
                $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
                $"Manual points changed from {previousPoints} to {dto.ManualPoints}",
                cancellationToken);

            return await MapToDtoAsync(existing, cancellationToken);
        }

        var entity = new KnowledgePoint(dto.DirectReportId, dto.ProjectId, dto.ManualPoints, dto.Notes);
        var created = await _knowledgePointRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ProjectKnowledge,
            created.Id,
            $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            $"Knowledge points created with {dto.ManualPoints} manual points",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<KnowledgePointDto> AddPointsAsync(AddKnowledgePointsDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(dto.DirectReportId, dto.ProjectId, cancellationToken);

        var existing = await _knowledgePointRepository.GetByDirectReportAndProjectAsync(
            dto.DirectReportId, dto.ProjectId, cancellationToken);

        var project = await _projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);

        if (existing is not null)
        {
            var previousPoints = existing.ManualPoints;
            existing.AddPoints(dto.PointsToAdd, dto.Notes);
            await _knowledgePointRepository.UpdateAsync(existing, cancellationToken);

            await _activityService.LogActivityAsync(
                ActivityType.Updated,
                EntityType.ProjectKnowledge,
                existing.Id,
                $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
                $"Added {dto.PointsToAdd} points (from {previousPoints} to {existing.ManualPoints})",
                cancellationToken);

            return await MapToDtoAsync(existing, cancellationToken);
        }

        // Create new if doesn't exist
        var entity = new KnowledgePoint(dto.DirectReportId, dto.ProjectId, dto.PointsToAdd, dto.Notes);
        var created = await _knowledgePointRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.ProjectKnowledge,
            created.Id,
            $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            $"Knowledge points created with {dto.PointsToAdd} manual points",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _knowledgePointRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(KnowledgePoint), id);
        }

        var project = await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _knowledgePointRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.ProjectKnowledge,
            id,
            $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            "Knowledge points record was deleted",
            cancellationToken);
    }

    public async Task ResetPointsAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var existing = await _knowledgePointRepository.GetByDirectReportAndProjectAsync(directReportId, projectId, cancellationToken);

        if (existing is null)
        {
            // No points to reset
            return;
        }

        var previousPoints = existing.ManualPoints;
        existing.UpdateManualPoints(0, null);
        await _knowledgePointRepository.UpdateAsync(existing, cancellationToken);

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(directReportId, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.ProjectKnowledge,
            existing.Id,
            $"Points: {project?.Name ?? "Unknown"} - {directReport?.FullName ?? "Unknown"}",
            $"Manual points reset from {previousPoints} to 0 after level increase",
            cancellationToken);
    }

    public async Task<int> CalculateAutomaticPointsAsync(Guid directReportId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var tasks = await _teamTaskRepository.GetByAssigneeIdAsync(directReportId, cancellationToken);
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);

        // Get project labels as a set for efficient matching
        var projectLabelSet = GetLabelSet(project?.Labels);

        var completedTasksForProject = tasks
            .Where(t => t.Status == TaskStatus.Done && IsTaskRelatedToProject(t, projectId, projectLabelSet))
            .ToList();

        return completedTasksForProject.Sum(t => t.StoryPoints ?? 1);
    }

    /// <summary>
    /// Determines if a task is related to a project either via direct ProjectId or shared labels.
    /// </summary>
    private static bool IsTaskRelatedToProject(TeamTask task, Guid projectId, HashSet<string> projectLabelSet)
    {
        // Check direct ProjectId match
        if (task.ProjectId == projectId)
        {
            return true;
        }

        // Check label-based match
        if (projectLabelSet.Count == 0)
        {
            return false;
        }

        var taskLabelSet = GetLabelSet(task.Labels);
        return taskLabelSet.Overlaps(projectLabelSet);
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

    public async Task<IReadOnlyList<KnowledgeLevelSuggestionDto>> GetLevelIncreaseSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var allKnowledgePoints = await _knowledgePointRepository.GetAllAsync(cancellationToken);
        var allKnowledge = await _projectKnowledgeRepository.GetAllAsync(cancellationToken);

        var suggestions = new List<KnowledgeLevelSuggestionDto>();

        var drLookup = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectLookup = projects.ToDictionary(p => p.Id, p => p.Name);

        // Check all combinations of direct reports and projects
        foreach (var dr in directReports.Where(d => d.IsDirect))
        {
            foreach (var project in projects)
            {
                var kp = allKnowledgePoints.FirstOrDefault(k => k.DirectReportId == dr.Id && k.ProjectId == project.Id);
                var automaticPoints = await CalculateAutomaticPointsAsync(dr.Id, project.Id, cancellationToken);
                var manualPoints = kp?.ManualPoints ?? 0;
                var totalPoints = manualPoints + automaticPoints;

                var currentKnowledge = allKnowledge.FirstOrDefault(k => k.DirectReportId == dr.Id && k.ProjectId == project.Id);
                var currentLevel = currentKnowledge?.KnowledgeLevel ?? 0;

                // Check if points meet threshold for next level
                var suggestedLevel = GetSuggestedLevel(currentLevel, totalPoints);
                if (suggestedLevel.HasValue)
                {
                    suggestions.Add(new KnowledgeLevelSuggestionDto
                    {
                        DirectReportId = dr.Id,
                        DirectReportName = drLookup.GetValueOrDefault(dr.Id, "Unknown"),
                        ProjectId = project.Id,
                        ProjectName = projectLookup.GetValueOrDefault(project.Id, "Unknown"),
                        TotalPoints = totalPoints,
                        CurrentLevel = currentLevel == 0 ? null : currentLevel,
                        SuggestedLevel = suggestedLevel.Value
                    });
                }
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Gets the suggested level based on current level and total points.
    /// Returns null if no level increase should be suggested.
    /// Level 0 and 1 are both considered the "starting level" (0-4 points).
    /// Thresholds: Level 2 at 5 points, Level 3 at 13 points, Level 4 at 21 points, Level 5 at 55 points.
    /// </summary>
    private static int? GetSuggestedLevel(int currentLevel, int totalPoints)
    {
        // Already at max level
        if (currentLevel >= 5)
            return null;

        // Level 0 and 1 are both considered "starting level"
        // When at level 0 or 1, the next level to suggest is 2
        var effectiveLevel = Math.Max(currentLevel, 1);
        var nextLevel = effectiveLevel + 1;

        // Check if points meet the threshold for the next level
        if (LevelThresholds.TryGetValue(nextLevel, out var threshold) && totalPoints >= threshold)
        {
            return nextLevel;
        }

        return null;
    }

    /// <summary>
    /// Checks if a level increase should be suggested based on current level and total points.
    /// </summary>
    private static bool ShouldSuggestLevelIncrease(int? currentLevel, int totalPoints)
    {
        return GetSuggestedLevel(currentLevel ?? 0, totalPoints).HasValue;
    }

    public async Task<ProjectKnowledgeMatrixWithPointsDto> GetMatrixWithPointsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var allScores = await _projectKnowledgeRepository.GetAllAsync(cancellationToken);
        var allPoints = await _knowledgePointRepository.GetAllAsync(cancellationToken);

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

        // Build points DTOs with calculated automatic points
        var pointDtos = new List<KnowledgePointDto>();
        var processedPairs = new HashSet<(Guid, Guid)>();

        // First, add existing knowledge point records
        foreach (var kp in allPoints)
        {
            var automaticPoints = await CalculateAutomaticPointsAsync(kp.DirectReportId, kp.ProjectId, cancellationToken);
            var totalPoints = kp.ManualPoints + automaticPoints;
            var knowledge = allScores.FirstOrDefault(k => k.DirectReportId == kp.DirectReportId && k.ProjectId == kp.ProjectId);

            pointDtos.Add(new KnowledgePointDto
            {
                Id = kp.Id,
                DirectReportId = kp.DirectReportId,
                DirectReportName = directReports.FirstOrDefault(dr => dr.Id == kp.DirectReportId)?.FullName ?? "Unknown",
                ProjectId = kp.ProjectId,
                ProjectName = projects.FirstOrDefault(p => p.Id == kp.ProjectId)?.Name ?? "Unknown",
                ManualPoints = kp.ManualPoints,
                AutomaticPoints = automaticPoints,
                TotalPoints = totalPoints,
                CurrentKnowledgeLevel = knowledge?.KnowledgeLevel,
                SuggestLevelIncrease = ShouldSuggestLevelIncrease(knowledge?.KnowledgeLevel, totalPoints),
                Notes = kp.Notes,
                CreatedAt = kp.CreatedAt,
                UpdatedAt = kp.UpdatedAt
            });

            processedPairs.Add((kp.DirectReportId, kp.ProjectId));
        }

        // Add computed points for combinations without explicit records but with automatic points
        foreach (var dr in directOnly)
        {
            foreach (var project in projects)
            {
                if (processedPairs.Contains((dr.Id, project.Id))) continue;

                var automaticPoints = await CalculateAutomaticPointsAsync(dr.Id, project.Id, cancellationToken);
                if (automaticPoints > 0)
                {
                    var knowledge = allScores.FirstOrDefault(k => k.DirectReportId == dr.Id && k.ProjectId == project.Id);

                    pointDtos.Add(new KnowledgePointDto
                    {
                        Id = Guid.Empty, // No stored record
                        DirectReportId = dr.Id,
                        DirectReportName = dr.FullName,
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        ManualPoints = 0,
                        AutomaticPoints = automaticPoints,
                        TotalPoints = automaticPoints,
                        CurrentKnowledgeLevel = knowledge?.KnowledgeLevel,
                        SuggestLevelIncrease = ShouldSuggestLevelIncrease(knowledge?.KnowledgeLevel, automaticPoints),
                        Notes = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = null
                    });
                }
            }
        }

        var suggestions = await GetLevelIncreaseSuggestionsAsync(cancellationToken);

        return new ProjectKnowledgeMatrixWithPointsDto
        {
            Projects = projectSummaries,
            DirectReports = directReportSummaries,
            Scores = scoreDtos,
            Points = pointDtos,
            Suggestions = suggestions.ToList()
        };
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

    private async Task<KnowledgePointDto> MapToDtoAsync(KnowledgePoint entity, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        var project = await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken);
        var automaticPoints = await CalculateAutomaticPointsAsync(entity.DirectReportId, entity.ProjectId, cancellationToken);
        var totalPoints = entity.ManualPoints + automaticPoints;

        var knowledge = await _projectKnowledgeRepository.GetByDirectReportAndProjectAsync(
            entity.DirectReportId, entity.ProjectId, cancellationToken);

        return new KnowledgePointDto
        {
            Id = entity.Id,
            DirectReportId = entity.DirectReportId,
            DirectReportName = directReport?.FullName ?? "Unknown",
            ProjectId = entity.ProjectId,
            ProjectName = project?.Name ?? "Unknown",
            ManualPoints = entity.ManualPoints,
            AutomaticPoints = automaticPoints,
            TotalPoints = totalPoints,
            CurrentKnowledgeLevel = knowledge?.KnowledgeLevel,
            SuggestLevelIncrease = ShouldSuggestLevelIncrease(knowledge?.KnowledgeLevel, totalPoints),
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<KnowledgePointDto>> MapToDtosAsync(IEnumerable<KnowledgePoint> entities, CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var allKnowledge = await _projectKnowledgeRepository.GetAllAsync(cancellationToken);

        var drLookup = directReports.ToDictionary(dr => dr.Id, dr => dr.FullName);
        var projectLookup = projects.ToDictionary(p => p.Id, p => p.Name);

        var result = new List<KnowledgePointDto>();

        foreach (var e in entities)
        {
            var automaticPoints = await CalculateAutomaticPointsAsync(e.DirectReportId, e.ProjectId, cancellationToken);
            var totalPoints = e.ManualPoints + automaticPoints;
            var knowledge = allKnowledge.FirstOrDefault(k => k.DirectReportId == e.DirectReportId && k.ProjectId == e.ProjectId);

            result.Add(new KnowledgePointDto
            {
                Id = e.Id,
                DirectReportId = e.DirectReportId,
                DirectReportName = drLookup.GetValueOrDefault(e.DirectReportId, "Unknown"),
                ProjectId = e.ProjectId,
                ProjectName = projectLookup.GetValueOrDefault(e.ProjectId, "Unknown"),
                ManualPoints = e.ManualPoints,
                AutomaticPoints = automaticPoints,
                TotalPoints = totalPoints,
                CurrentKnowledgeLevel = knowledge?.KnowledgeLevel,
                SuggestLevelIncrease = ShouldSuggestLevelIncrease(knowledge?.KnowledgeLevel, totalPoints),
                Notes = e.Notes,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
        }

        return result;
    }
}
