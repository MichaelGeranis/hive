using System.Text.Json;
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
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IActivityService _activityService;

    public TeamTaskService(
        ITeamTaskRepository taskRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        IParentRepository parentRepository,
        IAppSettingsRepository appSettingsRepository,
        IActivityService activityService)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _appSettingsRepository = appSettingsRepository ?? throw new ArgumentNullException(nameof(appSettingsRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
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

        return new TaskSummaryDto
        {
            TotalTasks = allTasks.Count,
            BacklogTasks = allTasks.Count(t => t.Status == TaskStatus.Backlog),
            TodoTasks = allTasks.Count(t => t.Status == TaskStatus.Todo),
            InProgressTasks = allTasks.Count(t => t.Status == TaskStatus.InProgress),
            InReviewTasks = allTasks.Count(t => t.Status == TaskStatus.InReview),
            DoneTasks = allTasks.Count(t => t.Status == TaskStatus.Done),
            CancelledTasks = allTasks.Count(t => t.Status == TaskStatus.Cancelled),
            OverdueTasks = allTasks.Count(t => t.IsOverdue()),
            UnassignedTasks = allTasks.Count(t => !t.AssigneeId.HasValue && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
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

        // Calculate estimated hours from story points using the mapping
        var estimatedHours = await CalculateEstimatedHoursAsync(dto.StoryPoints, cancellationToken);

        var entity = new TeamTask(
            dto.Title,
            dto.Description,
            dto.Type,
            dto.Priority,
            dto.AssigneeId,
            dto.ProjectId,
            dto.DueDate,
            estimatedHours,
            dto.StoryPoints,
            dto.Tags,
            dto.Labels,
            dto.Sprint,
            dto.TimeSpentMinutes,
            dto.ParentId);

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

        // Calculate estimated hours from story points using the mapping
        var estimatedHours = await CalculateEstimatedHoursAsync(dto.StoryPoints, cancellationToken);

        entity.Update(dto.Title, dto.Description, dto.Type, dto.Priority, dto.DueDate, estimatedHours, dto.StoryPoints, dto.Tags, dto.Labels, dto.Sprint, dto.TimeSpentMinutes);
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

    private async Task<TeamTaskDto> MapToDtoAsync(TeamTask entity, CancellationToken cancellationToken)
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
            AssigneeId = entity.AssigneeId,
            AssigneeName = assigneeName,
            ProjectId = entity.ProjectId,
            ProjectName = projectName,
            ParentId = entity.ParentId,
            ParentName = parentName,
            DueDate = entity.DueDate,
            EstimatedHours = entity.EstimatedHours,
            StoryPoints = entity.StoryPoints,
            Tags = entity.Tags,
            Labels = entity.Labels,
            Sprint = entity.Sprint,
            TimeSpentMinutes = entity.TimeSpentMinutes,
            IsOverdue = entity.IsOverdue(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<TeamTaskDto>> MapToDtosAsync(IReadOnlyList<TeamTask> entities, CancellationToken cancellationToken)
    {
        var result = new List<TeamTaskDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken));
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

    /// <summary>
    /// Calculates estimated hours from story points using the app settings mapping.
    /// If story points don't match exactly, uses the next biggest mapping (or last one if none bigger).
    /// </summary>
    private async Task<int?> CalculateEstimatedHoursAsync(int? storyPoints, CancellationToken cancellationToken)
    {
        if (!storyPoints.HasValue || storyPoints.Value <= 0)
            return null;

        var settings = await _appSettingsRepository.GetAsync(cancellationToken);
        if (settings is null || string.IsNullOrEmpty(settings.StoryPointMappings))
            return null;

        try
        {
            var mappings = JsonSerializer.Deserialize<List<StoryPointMapping>>(
                settings.StoryPointMappings,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (mappings is null || mappings.Count == 0)
                return null;

            var sortedMappings = mappings.OrderBy(m => m.Points).ToList();

            // Find exact match first
            var exactMatch = sortedMappings.FirstOrDefault(m => m.Points == storyPoints.Value);
            if (exactMatch is not null)
                return exactMatch.Hours;

            // Find next biggest mapping
            var nextBiggest = sortedMappings.FirstOrDefault(m => m.Points > storyPoints.Value);
            if (nextBiggest is not null)
                return nextBiggest.Hours;

            // If no bigger mapping exists, use the last (maximum) mapping
            return sortedMappings.Last().Hours;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record StoryPointMapping(int Points, int Hours, string? Label);
}
