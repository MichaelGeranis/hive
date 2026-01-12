using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Project management.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IActivityService _activityService;

    public ProjectService(
        IProjectRepository projectRepository,
        ITeamTaskRepository taskRepository,
        IParentRepository parentRepository,
        IActivityService activityService)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _projectRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (await _projectRepository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        var entity = new Project(dto.Name, dto.Description, dto.Labels, dto.Url);
        var created = await _projectRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Project,
            created.Id,
            created.Name,
            $"Project '{created.Name}' was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (await _projectRepository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        entity.Update(dto.Name, dto.Description, dto.Labels, dto.Url);
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Project,
            entity.Id,
            entity.Name,
            $"Project '{entity.Name}' was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }

        await _projectRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Project,
            id,
            entity.Name,
            $"Project '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<Project> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Project), id);
        }
        return entity;
    }

    private async Task<ProjectDto> MapToDtoAsync(Project entity, CancellationToken cancellationToken)
    {
        // Get tasks that match project labels (label-based relationship)
        var projectLabels = string.IsNullOrEmpty(entity.Labels)
            ? Array.Empty<string>()
            : entity.Labels.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var tasks = projectLabels.Length > 0
            ? await _taskRepository.GetByMatchingLabelsAsync(projectLabels, cancellationToken)
            : Array.Empty<TeamTask>();

        // Get parents that match project labels
        var parents = projectLabels.Length > 0
            ? await _parentRepository.GetByMatchingLabelsAsync(projectLabels, cancellationToken)
            : Array.Empty<Parent>();

        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var openTasks = tasks.Count(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

        return new ProjectDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Labels = entity.Labels,
            Url = entity.Url,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks,
            OpenTasks = openTasks,
            ParentCount = parents.Count
        };
    }

    private async Task<IReadOnlyList<ProjectDto>> MapToDtosAsync(IReadOnlyList<Project> entities, CancellationToken cancellationToken)
    {
        var result = new List<ProjectDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken));
        }
        return result;
    }
}
