using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for Sprint management.
/// </summary>
public class SprintService : ISprintService
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IActivityService _activityService;
    private readonly ISprintCapacityService _sprintCapacityService;

    public SprintService(
        ISprintRepository sprintRepository,
        IActivityService activityService,
        ISprintCapacityService sprintCapacityService)
    {
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _sprintCapacityService = sprintCapacityService ?? throw new ArgumentNullException(nameof(sprintCapacityService));
    }

    public async Task<SprintDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<SprintDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _sprintRepository.GetByNameAsync(name, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<SprintDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _sprintRepository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SprintDto>> GetByTeamAsync(string teamName, CancellationToken cancellationToken = default)
    {
        var entities = await _sprintRepository.GetByTeamAsync(teamName, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SprintDto>> GetByYearQuarterAsync(int year, int quarter, CancellationToken cancellationToken = default)
    {
        var entities = await _sprintRepository.GetByYearQuarterAsync(year, quarter, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SprintDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var entities = await _sprintRepository.GetByYearAsync(year, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<SprintDto> CreateAsync(CreateSprintDto dto, CancellationToken cancellationToken = default)
    {
        if (await _sprintRepository.ExistsAsync(dto.Name, cancellationToken))
        {
            throw new ConflictException($"A sprint with name '{dto.Name}' already exists.");
        }

        var entity = new Sprint(dto.Name);
        var created = await _sprintRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Sprint,
            created.Id,
            $"Sprint '{created.Name}'",
            $"Sprint '{created.Name}' was created",
            cancellationToken);

        return MapToDto(created);
    }

    public async Task<SprintDto> UpdateAsync(Guid id, UpdateSprintDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Sprint), id);
        }

        entity.UpdateDates(dto.StartDate, dto.EndDate);
        await _sprintRepository.UpdateAsync(entity, cancellationToken);

        await _sprintCapacityService.RecalculateAvailableMembersAsync(entity.Id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Sprint,
            entity.Id,
            $"Sprint '{entity.Name}'",
            $"Sprint '{entity.Name}' was updated",
            cancellationToken);

        return MapToDto(entity);
    }

    public async Task<SprintDto> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sprint name cannot be empty.", nameof(name));
        }

        var existing = await _sprintRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            return MapToDto(existing);
        }

        var entity = new Sprint(name);
        var created = await _sprintRepository.AddAsync(entity, cancellationToken);

        return MapToDto(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Sprint), id);
        }

        await _sprintRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Sprint,
            id,
            $"Sprint '{entity.Name}'",
            $"Sprint '{entity.Name}' was deleted",
            cancellationToken);
    }

    private static SprintDto MapToDto(Sprint entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        TeamName = entity.TeamName,
        Quarter = entity.Quarter,
        Year = entity.Year,
        SprintNumber = entity.SprintNumber,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
