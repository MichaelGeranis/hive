using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Project management.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITeamTaskRepository _taskRepository;

    public ProjectService(IProjectRepository projectRepository, ITeamTaskRepository taskRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
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

    public async Task<IReadOnlyList<ProjectDto>> GetByStatusAsync(ProjectStatus status, CancellationToken cancellationToken = default)
    {
        var entities = await _projectRepository.GetByStatusAsync(status, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _projectRepository.GetActiveAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (await _projectRepository.NameExistsAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        var entity = new Project(dto.Name, dto.Description, dto.StartDate, dto.TargetEndDate);
        var created = await _projectRepository.AddAsync(entity, cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        if (await _projectRepository.NameExistsAsync(dto.Name, id, cancellationToken))
        {
            throw new ConflictException($"A project with name '{dto.Name}' already exists.");
        }

        entity.Update(dto.Name, dto.Description, dto.StartDate, dto.TargetEndDate);
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<ProjectDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Activate();
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<ProjectDto> PutOnHoldAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.PutOnHold();
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<ProjectDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Complete();
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<ProjectDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Cancel();
        await _projectRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _projectRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), id);
        }

        await _projectRepository.DeleteAsync(id, cancellationToken);
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
        var tasks = await _taskRepository.GetByProjectIdAsync(entity.Id, cancellationToken);
        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var openTasks = tasks.Count(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

        return new ProjectDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            StatusName = GetStatusName(entity.Status),
            StartDate = entity.StartDate,
            TargetEndDate = entity.TargetEndDate,
            ActualEndDate = entity.ActualEndDate,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            TotalTasks = tasks.Count,
            CompletedTasks = completedTasks,
            OpenTasks = openTasks
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

    private static string GetStatusName(ProjectStatus status) => status switch
    {
        ProjectStatus.Planning => "Planning",
        ProjectStatus.Active => "Active",
        ProjectStatus.OnHold => "On Hold",
        ProjectStatus.Completed => "Completed",
        ProjectStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
}
