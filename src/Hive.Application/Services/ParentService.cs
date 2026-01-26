using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Parent management.
/// </summary>
public class ParentService : IParentService
{
    private readonly IParentRepository _parentRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IActivityService _activityService;

    public ParentService(IParentRepository parentRepository, ITeamTaskRepository taskRepository, IActivityService activityService)
    {
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<ParentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _parentRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<ParentDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _parentRepository.GetByNameAsync(name, cancellationToken);
        return entity is null ? null : await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<ParentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _parentRepository.GetAllAsync(cancellationToken);

        // Get all parent names for exclusion logic
        var parentNames = entities.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<ParentDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, parentNames, cancellationToken));
        }
        return result;
    }

    public async Task<ParentDto> CreateAsync(CreateParentDto dto, CancellationToken cancellationToken = default)
    {
        if (await _parentRepository.ExistsAsync(dto.Name, cancellationToken))
        {
            throw new ConflictException($"A parent with name '{dto.Name}' already exists.");
        }

        var entity = new Parent(dto.Name, dto.Labels);
        var created = await _parentRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Parent,
            created.Id,
            $"Parent '{created.Name}'",
            $"Parent '{created.Name}' was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<ParentDto> GetOrCreateAsync(string name, int? timeSpentMinutes = null, Guid? teamTaskId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parent name cannot be empty.", nameof(name));
        }

        var existing = await _parentRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            var needsUpdate = false;

            // Update time spent if provided
            if (timeSpentMinutes.HasValue)
            {
                existing.UpdateTimeSpent(timeSpentMinutes);
                needsUpdate = true;
            }

            // Link to task if provided and not already linked
            if (teamTaskId.HasValue && !existing.TeamTaskId.HasValue)
            {
                existing.LinkToTask(teamTaskId.Value);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                await _parentRepository.UpdateAsync(existing, cancellationToken);
            }

            return await MapToDtoAsync(existing, cancellationToken);
        }

        var entity = new Parent(name, null, timeSpentMinutes, teamTaskId);
        var created = await _parentRepository.AddAsync(entity, cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<ParentDto> UpdateAsync(Guid id, UpdateParentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _parentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Parent), id);
        }

        // Check for duplicate name if name is being changed
        if (!entity.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (await _parentRepository.ExistsAsync(dto.Name, cancellationToken))
            {
                throw new ConflictException($"A parent with name '{dto.Name}' already exists.");
            }
        }

        entity.Update(dto.Name, dto.Labels);
        await _parentRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Parent,
            entity.Id,
            $"Parent '{entity.Name}'",
            $"Parent '{entity.Name}' was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _parentRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Parent), id);
        }

        await _parentRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Parent,
            id,
            $"Parent '{entity.Name}'",
            $"Parent '{entity.Name}' was deleted",
            cancellationToken);
    }

    private async Task<ParentDto> MapToDtoAsync(Parent entity, CancellationToken cancellationToken)
    {
        // Get all parent names for exclusion logic
        var allParents = await _parentRepository.GetAllAsync(cancellationToken);
        var parentNames = allParents.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await MapToDtoAsync(entity, parentNames, cancellationToken);
    }

    private async Task<ParentDto> MapToDtoAsync(Parent entity, HashSet<string> parentNames, CancellationToken cancellationToken)
    {
        // Get all tasks assigned to this parent
        var tasks = await _taskRepository.GetByParentIdAsync(entity.Id, cancellationToken);

        // Exclude tasks that are also parents (by matching title to parent names)
        // This ensures tasks that represent parent items themselves don't count toward metrics
        var nonParentTasks = tasks.Where(t => !parentNames.Contains(t.Title)).ToList();

        var completedTasks = nonParentTasks.Count(t => t.Status == TaskStatus.Done);
        var openTasks = nonParentTasks.Count(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);
        var totalStoryPoints = nonParentTasks.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
        var totalTimeSpent = nonParentTasks.Where(t => t.TimeSpentMinutes.HasValue).Sum(t => t.TimeSpentMinutes!.Value);

        return new ParentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Labels = entity.Labels,
            TotalTasks = nonParentTasks.Count,
            CompletedTasks = completedTasks,
            OpenTasks = openTasks,
            TotalStoryPoints = totalStoryPoints,
            TotalTimeSpentMinutes = totalTimeSpent,
            TimeSpentMinutes = entity.TimeSpentMinutes,
            TeamTaskId = entity.TeamTaskId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
