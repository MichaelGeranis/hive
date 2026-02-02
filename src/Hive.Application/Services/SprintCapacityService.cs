using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for SprintCapacity management.
/// </summary>
public class SprintCapacityService : ISprintCapacityService
{
    private readonly ISprintCapacityRepository _capacityRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    public SprintCapacityService(
        ISprintCapacityRepository capacityRepository,
        ISprintRepository sprintRepository,
        ILeaveRepository leaveRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _capacityRepository = capacityRepository ?? throw new ArgumentNullException(nameof(capacityRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _leaveRepository = leaveRepository ?? throw new ArgumentNullException(nameof(leaveRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<SprintCapacityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _capacityRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<SprintCapacityDto?> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var entity = await _capacityRepository.GetBySprintIdAsync(sprintId, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<SprintCapacityDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _capacityRepository.GetAllAsync(cancellationToken);
        var result = new List<SprintCapacityDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken));
        }
        return result;
    }

    public async Task<SprintCapacityDto> CreateOrUpdateAsync(CreateSprintCapacityDto dto, CancellationToken cancellationToken = default)
    {
        // Verify sprint exists
        var sprint = await _sprintRepository.GetByIdAsync(dto.SprintId, cancellationToken);
        if (sprint is null)
        {
            throw new NotFoundException(nameof(Sprint), dto.SprintId);
        }

        var computedAvailableMembers = await ComputeAvailableMembersAsync(sprint, cancellationToken);

        // Check if capacity already exists for this sprint
        var existing = await _capacityRepository.GetBySprintIdAsync(dto.SprintId, cancellationToken);
        if (existing is not null)
        {
            existing.Update(dto.TotalCapacityPoints, computedAvailableMembers);
            await _capacityRepository.UpdateAsync(existing, cancellationToken);

            await _activityService.LogActivityAsync(
                ActivityType.Updated,
                EntityType.SprintCapacity,
                existing.Id,
                $"Capacity for '{sprint.Name}'",
                $"Sprint capacity for '{sprint.Name}' was updated",
                cancellationToken);

            return await MapToDtoAsync(existing, cancellationToken);
        }

        // Create new capacity
        var entity = new SprintCapacity(dto.SprintId, dto.TotalCapacityPoints, computedAvailableMembers);
        var created = await _capacityRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.SprintCapacity,
            created.Id,
            $"Capacity for '{sprint.Name}'",
            $"Sprint capacity for '{sprint.Name}' was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _capacityRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(SprintCapacity), id);
        }

        var sprint = await _sprintRepository.GetByIdAsync(entity.SprintId, cancellationToken);

        await _capacityRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.SprintCapacity,
            id,
            $"Capacity for '{sprint?.Name ?? "Unknown"}'",
            $"Sprint capacity for '{sprint?.Name ?? "Unknown"}' was deleted",
            cancellationToken);
    }

    public async Task RecalculateAvailableMembersAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId, cancellationToken);
        if (sprint is null) return;

        var computedAvailableMembers = await ComputeAvailableMembersAsync(sprint, cancellationToken);

        var existing = await _capacityRepository.GetBySprintIdAsync(sprintId, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateAvailableMembers(computedAvailableMembers);
            await _capacityRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var entity = new SprintCapacity(sprintId, 0, computedAvailableMembers);
            await _capacityRepository.AddAsync(entity, cancellationToken);
        }
    }

    public async Task RecalculateForDateRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var allSprints = await _sprintRepository.GetAllAsync(cancellationToken);

        foreach (var sprint in allSprints)
        {
            var sprintStart = sprint.GetEstimatedStartDate();
            var sprintEnd = sprint.GetEstimatedEndDate();

            // Check if sprint date range overlaps [start, end]
            if (sprintStart <= end && sprintEnd >= start)
            {
                await RecalculateAvailableMembersAsync(sprint.Id, cancellationToken);
            }
        }
    }

    private async Task<int> ComputeAvailableMembersAsync(Sprint sprint, CancellationToken cancellationToken)
    {
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var totalTeamSize = directReports.Count(dr => dr.IsDirect);

        if (totalTeamSize == 0) return 0;

        var sprintStart = sprint.GetEstimatedStartDate();
        var sprintEnd = sprint.GetEstimatedEndDate();
        var workingDaysInSprint = GetWorkingDays(sprintStart, sprintEnd);

        if (workingDaysInSprint == 0) return totalTeamSize;

        var directReportIds = directReports.Where(dr => dr.IsDirect).Select(dr => dr.Id).ToHashSet();
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var activeLeaves = allLeaves
            .Where(l => l.Status == LeaveStatus.Active && directReportIds.Contains(l.DirectReportId))
            .ToList();

        var totalLeaveDays = 0;
        foreach (var leave in activeLeaves)
        {
            if (!leave.OverlapsWith(sprintStart, sprintEnd))
                continue;

            var overlapStart = leave.StartDate > sprintStart ? leave.StartDate : sprintStart;
            var overlapEnd = leave.EndDate < sprintEnd ? leave.EndDate : sprintEnd;
            totalLeaveDays += GetWorkingDays(overlapStart, overlapEnd);
        }

        var lostCapacity = (double)totalLeaveDays / workingDaysInSprint;
        return (int)Math.Floor(Math.Max(0, totalTeamSize - lostCapacity));
    }

    private static int GetWorkingDays(DateTime start, DateTime end)
    {
        int count = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private async Task<SprintCapacityDto> MapToDtoAsync(SprintCapacity entity, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(entity.SprintId, cancellationToken);

        return new SprintCapacityDto
        {
            Id = entity.Id,
            SprintId = entity.SprintId,
            SprintName = sprint?.Name ?? "Unknown",
            TotalCapacityPoints = entity.TotalCapacityPoints,
            AvailableMembers = entity.AvailableMembers,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
