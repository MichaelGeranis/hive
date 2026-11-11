using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for TeamTask management.
/// </summary>
public class TeamTaskService : ITeamTaskService
{
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IActivityService _activityService;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ITaskReadinessReviewRepository _reviewRepository;

    public TeamTaskService(
        ITeamTaskRepository taskRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        IParentRepository parentRepository,
        IActivityService activityService,
        IAppSettingsRepository appSettingsRepository,
        IProductRepository productRepository,
        ISprintRepository sprintRepository,
        ITaskReadinessReviewRepository reviewRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _appSettingsRepository = appSettingsRepository ?? throw new ArgumentNullException(nameof(appSettingsRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
    }

    private async Task<int> GetStaleTaskDaysAsync(CancellationToken cancellationToken)
    {
        var settings = await _appSettingsRepository.GetAsync(cancellationToken);
        return settings?.StaleTaskDays ?? TeamTask.DefaultStaleAfterDays;
    }

    public async Task<TeamTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<PagedResult<TeamTaskDto>> GetAllPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await _taskRepository.GetAllPagedAsync(pagination.Skip, pagination.PageSize, cancellationToken);
        var dtos = await MapToDtosAsync(entities, cancellationToken);
        return PagedResult<TeamTaskDto>.Create(dtos, totalCount, pagination);
    }

    public async Task<TaskPagedResult> GetFilteredPagedAsync(TaskPaginationParams pagination, CancellationToken cancellationToken = default)
    {
        // Load parent names for exclusion if requested
        IEnumerable<string>? excludeTaskTitles = null;
        HashSet<string>? parentNames = null;
        if (pagination.ExcludeParents)
        {
            var parents = await _parentRepository.GetAllAsync(cancellationToken);
            parentNames = new HashSet<string>(parents.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            excludeTaskTitles = parentNames;
        }

        DateTime? staleBefore = null;
        if (pagination.Filter == TaskFilter.Stale)
        {
            staleBefore = TeamTask.StaleCutoff(DateTime.UtcNow, await GetStaleTaskDaysAsync(cancellationToken));
        }

        IReadOnlyCollection<Product>? unattributedAmong = null;
        if (pagination.Unattributed)
        {
            unattributedAmong = await _productRepository.GetAllAsync(cancellationToken);
        }

        IReadOnlyCollection<Guid>? ids = null;
        if (pagination.Unestimated)
        {
            var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
            var allParents = await _parentRepository.GetAllAsync(cancellationToken);
            var team = await _directReportRepository.GetAllAsync(cancellationToken);
            ids = TeamTask.FindMissingStoryPoints(allTasks, allParents, team).Select(t => t.Id).ToList();
        }

        if (pagination.NoAcceptanceCriteria)
        {
            var allTasks = await _taskRepository.GetAllAsync(cancellationToken);
            var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
            var missing = TaskReadiness.FindUpcoming(allTasks, sprints, DateTime.UtcNow.Date)
                .Where(t => t.IsMissingAcceptanceCriteria())
                .Select(t => t.Id);
            ids = ids is null ? missing.ToList() : ids.Intersect(missing).ToList();
        }

        var filter = new TeamTaskFilter
        {
            Status = pagination.Status,
            Type = pagination.Type,
            Overdue = pagination.Filter == TaskFilter.Overdue,
            StaleBefore = staleBefore,
            AssigneeId = pagination.AssigneeId,
            Unassigned = pagination.Unassigned,
            SearchTerm = pagination.SearchTerm,
            Label = pagination.Label,
            Component = pagination.Component,
            UnattributedAmong = unattributedAmong,
            Ids = ids,
            Sprint = pagination.Sprint,
            ExcludeTaskTitles = excludeTaskTitles
        };

        var (entities, totalCount, totalStoryPoints) = await _taskRepository.GetFilteredPagedAsync(
            pagination.Skip,
            pagination.PageSize,
            filter,
            cancellationToken);
        var dtos = await MapToDtosAsync(entities, cancellationToken, parentNames);
        return new TaskPagedResult
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalStoryPoints = totalStoryPoints
        };
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetByAssigneeIdAsync(Guid assigneeId, CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetByAssigneeIdAsync(assigneeId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetByStatusAsync(status, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetByPriorityAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetByPriorityAsync(priority, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetOverdueAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamTaskDto>> GetUnassignedAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _taskRepository.GetUnassignedAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<TaskSummaryDto> GetSummaryAsync(Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var allTasks = projectId.HasValue
            ? await _taskRepository.GetByProjectIdAsync(projectId.Value, cancellationToken)
            : await _taskRepository.GetAllAsync(cancellationToken);

        // Extract unique labels
        var allLabels = allTasks
            .Where(t => !string.IsNullOrEmpty(t.Labels))
            .SelectMany(t => t.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        // Each sprint once, even for work carried over across several, newest first so the
        // sprints that matter now are the ones shown before the list collapses
        var allSprints = allTasks
            .SelectMany(t => t.GetSprintNames())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new Sprint(name))
            .OrderByDescending(s => s.GetOrderingKey())
            .Select(s => s.Name)
            .ToList();

        var now = DateTime.UtcNow;
        var staleTaskDays = await GetStaleTaskDaysAsync(cancellationToken);

        return new TaskSummaryDto
        {
            TotalTasks = allTasks.Count,
            BacklogTasks = allTasks.Count(t => t.Status == TaskStatus.Backlog),
            TodoTasks = allTasks.Count(t => t.Status == TaskStatus.Todo),
            BlockedTasks = allTasks.Count(t => t.Status == TaskStatus.Blocked),
            InProgressTasks = allTasks.Count(t => t.Status == TaskStatus.InProgress),
            InReviewTasks = allTasks.Count(t => t.Status == TaskStatus.InReview),
            InTestTasks = allTasks.Count(t => t.Status == TaskStatus.InTest),
            POAcceptanceTasks = allTasks.Count(t => t.Status == TaskStatus.POAcceptance),
            ReadyToReleaseTasks = allTasks.Count(t => t.Status == TaskStatus.ReadyToRelease),
            DoneTasks = allTasks.Count(t => t.Status == TaskStatus.Done),
            CancelledTasks = allTasks.Count(t => t.Status == TaskStatus.Cancelled),
            OverdueTasks = allTasks.Count(t => t.IsOverdue()),
            StaleTasks = allTasks.Count(t => t.IsStale(now, staleTaskDays)),
            StaleTaskDays = staleTaskDays,
            UnassignedTasks = allTasks.Count(t => !t.AssigneeId.HasValue && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled),
            AllLabels = allLabels,
            AllSprints = allSprints
        };
    }

    public async Task<TeamTaskDto> CreateAsync(CreateTeamTaskDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.AssigneeId.HasValue)
        {
            var assignee = await _directReportRepository.GetByIdAsync(dto.AssigneeId.Value, cancellationToken);
            if (assignee is null)
            {
                throw new NotFoundException(nameof(DirectReport), dto.AssigneeId.Value);
            }
        }

        if (dto.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value, cancellationToken);
            if (project is null)
            {
                throw new NotFoundException(nameof(Project), dto.ProjectId.Value);
            }
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _parentRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new NotFoundException(nameof(Parent), dto.ParentId.Value);
            }
        }

        var entity = new TeamTask(
            dto.Title,
            description: dto.Description,
            type: dto.Type,
            priority: dto.Priority,
            assigneeId: dto.AssigneeId,
            projectId: dto.ProjectId,
            dueDate: dto.DueDate,
            storyPoints: dto.StoryPoints,
            tags: dto.Tags,
            labels: dto.Labels,
            components: dto.Components,
            sprint: dto.Sprint,
            parentId: dto.ParentId);

        var created = await _taskRepository.AddAsync(entity, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Task,
            created.Id,
            $"Task - {created.Title}",
            "New task created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<TeamTaskDto> UpdateAsync(Guid id, UpdateTeamTaskDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (dto.ParentId.HasValue)
        {
            var parent = await _parentRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                throw new NotFoundException(nameof(Parent), dto.ParentId.Value);
            }
        }

        entity.Update(dto.Title, description: dto.Description, type: dto.Type, priority: dto.Priority, dueDate: dto.DueDate, storyPoints: dto.StoryPoints, tags: dto.Tags, labels: dto.Labels, components: dto.Components, sprint: dto.Sprint);
        entity.AssignToParent(dto.ParentId);
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Task,
            entity.Id,
            entity.Title,
            $"Task '{entity.Title}' was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> AssignAsync(Guid id, AssignTaskDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (dto.AssigneeId.HasValue)
        {
            var assignee = await _directReportRepository.GetByIdAsync(dto.AssigneeId.Value, cancellationToken);
            if (assignee is null)
            {
                throw new NotFoundException(nameof(DirectReport), dto.AssigneeId.Value);
            }
        }

        entity.AssignTo(dto.AssigneeId);
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> MoveToBacklogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.MoveToBacklog();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> MoveToTodoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.MoveToTodo();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Start();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> MoveToReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.MoveToReview();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Complete();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        // Log activity
        await _activityService.LogActivityAsync(
            ActivityType.Completed,
            EntityType.Task,
            entity.Id,
            $"Task - {entity.Title}",
            "Task completed",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Cancel();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Cancelled,
            EntityType.Task,
            entity.Id,
            entity.Title,
            $"Task '{entity.Title}' was cancelled",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> ReopenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Reopen();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> OverrideFieldsAsync(Guid id, OverrideTeamTaskFieldsDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (dto.HasAssigneeOverride)
        {
            if (dto.AssigneeId.HasValue)
            {
                var assignee = await _directReportRepository.GetByIdAsync(dto.AssigneeId.Value, cancellationToken);
                if (assignee is null)
                {
                    throw new NotFoundException(nameof(DirectReport), dto.AssigneeId.Value);
                }
            }
            entity.OverrideAssignee(dto.AssigneeId);
        }

        if (dto.HasStoryPointsOverride)
        {
            entity.OverrideStoryPoints(dto.StoryPoints);
        }

        if (dto.HasPreviousSprintsStoryPointsOverride)
        {
            entity.OverridePreviousSprintsStoryPoints(dto.PreviousSprintsStoryPoints);
        }

        if (dto.HasSprintOverride)
        {
            entity.OverrideSprint(dto.Sprint);
        }

        await _taskRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Task,
            entity.Id,
            entity.Title,
            $"Task '{entity.Title}' fields were overridden",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> ClearOverridesAsync(Guid id, ClearTeamTaskOverridesDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        foreach (var field in dto.Fields)
        {
            entity.ClearOverride(field);
        }

        await _taskRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Task,
            entity.Id,
            entity.Title,
            $"Task '{entity.Title}' overrides were cleared for: {string.Join(", ", dto.Fields)}",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await GetEntityOrThrowAsync(id, cancellationToken);

        // Create a new task with the same properties (excluding ID and overrides)
        var duplicated = new TeamTask(
            $"{source.Title} (Copy)",
            description: source.Description,
            type: source.Type,
            priority: source.Priority,
            assigneeId: source.AssigneeId,
            projectId: source.ProjectId,
            dueDate: source.DueDate,
            storyPoints: source.StoryPoints,
            tags: source.Tags,
            labels: source.Labels,
            components: source.Components,
            sprint: source.Sprint,
            parentId: source.ParentId);

        var created = await _taskRepository.AddAsync(duplicated, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Task,
            created.Id,
            $"Task - {created.Title}",
            $"Task duplicated from '{source.Title}'",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(TeamTask), id);
        }

        await _taskRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Task,
            id,
            entity.Title,
            $"Task '{entity.Title}' was deleted",
            cancellationToken);
    }

    public async Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idsList = ids.ToList();

        // Validate that all tasks exist before deleting any
        foreach (var id in idsList)
        {
            if (!await _taskRepository.ExistsAsync(id, cancellationToken))
            {
                throw new NotFoundException(nameof(TeamTask), id);
            }
        }

        await _taskRepository.DeleteManyAsync(idsList, cancellationToken);
    }

    private async Task<TeamTask> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(TeamTask), id);
        }
        return entity;
    }

    private async Task<TeamTaskDto> MapToDtoAsync(
        TeamTask entity,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? parentNames = null,
        IReadOnlyDictionary<Guid, TaskReadinessReview>? reviews = null)
    {
        string? assigneeName = null;
        string? projectName = null;
        string? parentName = null;

        if (entity.AssigneeId.HasValue)
        {
            var assignee = await _directReportRepository.GetByIdAsync(entity.AssigneeId.Value, cancellationToken);
            assigneeName = assignee?.FullName;
        }

        if (entity.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(entity.ProjectId.Value, cancellationToken);
            projectName = project?.Name;
        }

        if (entity.ParentId.HasValue)
        {
            var parent = await _parentRepository.GetByIdAsync(entity.ParentId.Value, cancellationToken);
            parentName = parent?.Name;
        }

        var review = reviews is not null
            ? reviews.GetValueOrDefault(entity.Id)
            : await _reviewRepository.GetByTaskIdAsync(entity.Id, cancellationToken);

        // Calculate new sprints story points
        int? newSprintsStoryPoints = null;
        if (entity.StoryPoints.HasValue)
        {
            if (entity.PreviousSprintsStoryPoints.HasValue)
            {
                newSprintsStoryPoints = Math.Max(0, entity.StoryPoints.Value - entity.PreviousSprintsStoryPoints.Value);
            }
            else
            {
                newSprintsStoryPoints = entity.StoryPoints.Value;
            }
        }

        return new TeamTaskDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Type = entity.Type,
            TypeName = GetTypeName(entity.Type),
            Priority = entity.Priority,
            PriorityName = GetPriorityName(entity.Priority),
            Status = entity.Status,
            StatusName = GetStatusName(entity.Status),
            StatusChangedAt = entity.StatusChangedAt,
            AssigneeId = entity.AssigneeId,
            AssigneeName = assigneeName,
            ProjectId = entity.ProjectId,
            ProjectName = projectName,
            ParentId = entity.ParentId,
            ParentName = parentName,
            DueDate = entity.DueDate,
            StoryPoints = entity.StoryPoints,
            Tags = entity.Tags,
            Labels = entity.Labels,
            Components = entity.Components,
            Sprint = entity.Sprint,
            PreviousSprintsStoryPoints = entity.PreviousSprintsStoryPoints,
            NewSprintsStoryPoints = newSprintsStoryPoints,
            OverriddenFields = entity.OverriddenFields,
            IsOverdue = entity.IsOverdue(),
            IsParentTask = parentNames?.Contains(entity.Title) ?? false,
            AiReview = TaskReadinessService.MapReview(review, entity),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<TeamTaskDto>> MapToDtosAsync(IReadOnlyList<TeamTask> entities, CancellationToken cancellationToken, HashSet<string>? preloadedParentNames = null)
    {
        // Load parent names for IsParentTask flag
        var parentNames = preloadedParentNames;
        if (parentNames == null)
        {
            var parents = await _parentRepository.GetAllAsync(cancellationToken);
            parentNames = parents != null
                ? new HashSet<string>(parents.Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var reviews = (await _reviewRepository.GetByTaskIdsAsync(entities.Select(e => e.Id).ToList(), cancellationToken) ?? [])
            .ToDictionary(r => r.TeamTaskId);

        var result = new List<TeamTaskDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken, parentNames, reviews));
        }
        return result;
    }

    private static string GetTypeName(TaskType type) => type switch
    {
        TaskType.Task => "Task",
        TaskType.Epic => "Epic",
        TaskType.Story => "Story",
        TaskType.SubTask => "Sub-task",
        TaskType.Bug => "Bug",
        TaskType.Spike => "Spike",
        TaskType.Support => "Support",
        _ => "Unknown"
    };

    private static string GetPriorityName(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Low",
        TaskPriority.Medium => "Medium",
        TaskPriority.High => "High",
        TaskPriority.Critical => "Critical",
        _ => "Unknown"
    };

    private static string GetStatusName(TaskStatus status) => status switch
    {
        TaskStatus.Backlog => "Backlog",
        TaskStatus.Todo => "To Do",
        TaskStatus.Blocked => "Blocked",
        TaskStatus.InProgress => "In Progress",
        TaskStatus.InReview => "In Review",
        TaskStatus.InTest => "In Test",
        TaskStatus.POAcceptance => "PO Acceptance",
        TaskStatus.ReadyToRelease => "Ready to Release",
        TaskStatus.Done => "Done",
        TaskStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
}
