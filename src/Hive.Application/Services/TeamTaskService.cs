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
    private readonly IAppSettingsRepository _appSettingsRepository;

    public TeamTaskService(
        ITeamTaskRepository taskRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        IAppSettingsRepository appSettingsRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _appSettingsRepository = appSettingsRepository ?? throw new ArgumentNullException(nameof(appSettingsRepository));
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
            dto.TimeSpentMinutes);

        var created = await _taskRepository.AddAsync(entity, cancellationToken);
        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<TeamTaskDto> UpdateAsync(Guid id, UpdateTeamTaskDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        // Calculate estimated hours from story points using the mapping
        var estimatedHours = await CalculateEstimatedHoursAsync(dto.StoryPoints, cancellationToken);

        entity.Update(dto.Title, dto.Description, dto.Type, dto.Priority, dto.DueDate, estimatedHours, dto.StoryPoints, dto.Tags, dto.Labels, dto.Sprint, dto.TimeSpentMinutes);
        await _taskRepository.UpdateAsync(entity, cancellationToken);

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

    public async Task<TeamTaskDto> CompleteAsync(Guid id, CompleteTaskDto? dto = null, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Complete(dto?.ActualHours);
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Cancel();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> ReopenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Reopen();
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<TeamTaskDto> LogHoursAsync(Guid id, LogHoursDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.LogHours(dto.Hours);
        await _taskRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _taskRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(TeamTask), id);
        }

        await _taskRepository.DeleteAsync(id, cancellationToken);
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
            DueDate = entity.DueDate,
            EstimatedHours = entity.EstimatedHours,
            StoryPoints = entity.StoryPoints,
            ActualHours = entity.ActualHours,
            Tags = entity.Tags,
            Labels = entity.Labels,
            Sprint = entity.Sprint,
            TimeSpentMinutes = entity.TimeSpentMinutes,
            IsOverdue = entity.IsOverdue(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt
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
        TaskStatus.InProgress => "In Progress",
        TaskStatus.InReview => "In Review",
        TaskStatus.Done => "Done",
        TaskStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    /// <summary>
    /// Calculates estimated hours from story points using the app settings mapping.
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

            // Find exact match first
            var mapping = mappings.FirstOrDefault(m => m.Points == storyPoints.Value);
            if (mapping is not null)
                return mapping.Hours;

            // If no exact match, interpolate or use closest
            var sortedMappings = mappings.OrderBy(m => m.Points).ToList();

            // If below minimum, use minimum's ratio
            if (storyPoints.Value < sortedMappings.First().Points)
            {
                var first = sortedMappings.First();
                var ratio = (double)first.Hours / first.Points;
                return (int)Math.Round(storyPoints.Value * ratio);
            }

            // If above maximum, use maximum's ratio
            if (storyPoints.Value > sortedMappings.Last().Points)
            {
                var last = sortedMappings.Last();
                var ratio = (double)last.Hours / last.Points;
                return (int)Math.Round(storyPoints.Value * ratio);
            }

            // Linear interpolation between two closest points
            var lower = sortedMappings.LastOrDefault(m => m.Points <= storyPoints.Value);
            var upper = sortedMappings.FirstOrDefault(m => m.Points >= storyPoints.Value);

            if (lower is not null && upper is not null && lower.Points != upper.Points)
            {
                var ratio = (double)(storyPoints.Value - lower.Points) / (upper.Points - lower.Points);
                return (int)Math.Round(lower.Hours + ratio * (upper.Hours - lower.Hours));
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record StoryPointMapping(int Points, int Hours, string? Label);
}
