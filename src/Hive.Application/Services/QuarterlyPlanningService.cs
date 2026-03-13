using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using System.Drawing;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing quarterly planning operations.
/// </summary>
public class QuarterlyPlanningService : IQuarterlyPlanningService
{
    private readonly IQuarterRepository _quarterRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ISprintGoalRepository _sprintGoalRepository;
    private readonly IInitiativeDependencyRepository _dependencyRepository;
    private readonly IInitiativeMemberRepository _initiativeMemberRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IActivityService _activityService;
    private readonly IAppSettingsService _appSettingsService;

    public QuarterlyPlanningService(
        IQuarterRepository quarterRepository,
        IInitiativeRepository initiativeRepository,
        IAllocationRepository allocationRepository,
        ISprintGoalRepository sprintGoalRepository,
        IInitiativeDependencyRepository dependencyRepository,
        IInitiativeMemberRepository initiativeMemberRepository,
        ISprintRepository sprintRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        ILeaveRepository leaveRepository,
        IActivityService activityService,
        IAppSettingsService appSettingsService)
    {
        _quarterRepository = quarterRepository ?? throw new ArgumentNullException(nameof(quarterRepository));
        _initiativeRepository = initiativeRepository ?? throw new ArgumentNullException(nameof(initiativeRepository));
        _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
        _sprintGoalRepository = sprintGoalRepository ?? throw new ArgumentNullException(nameof(sprintGoalRepository));
        _dependencyRepository = dependencyRepository ?? throw new ArgumentNullException(nameof(dependencyRepository));
        _initiativeMemberRepository = initiativeMemberRepository ?? throw new ArgumentNullException(nameof(initiativeMemberRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _leaveRepository = leaveRepository ?? throw new ArgumentNullException(nameof(leaveRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
    }

    #region Quarter Operations

    public async Task<QuarterDto?> GetQuarterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapQuarterToDto(entity);
    }

    public async Task<QuarterDto?> GetQuarterByYearQuarterAsync(int year, int quarterNumber, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByYearQuarterAsync(year, quarterNumber, cancellationToken);
        return entity is null ? null : MapQuarterToDto(entity);
    }

    public async Task<IReadOnlyList<QuarterDto>> GetAllQuartersAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _quarterRepository.GetAllAsync(cancellationToken);
        return entities.Select(MapQuarterToDto).ToList();
    }

    public async Task<QuarterDto?> GetActiveQuarterAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetActiveAsync(cancellationToken);
        return entity is null ? null : MapQuarterToDto(entity);
    }

    public async Task<QuarterDto> CreateQuarterAsync(CreateQuarterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _quarterRepository.ExistsAsync(dto.Year, dto.QuarterNumber, cancellationToken))
        {
            throw new ConflictException($"Quarter Q{dto.QuarterNumber} {dto.Year} already exists.");
        }

        var entity = new Quarter(dto.Year, dto.QuarterNumber, dto.OkrReference);
        var created = await _quarterRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Quarter,
            created.Id,
            created.Name,
            $"Quarter '{created.Name}' was created",
            cancellationToken);

        return MapQuarterToDto(created);
    }

    public async Task<QuarterDto> UpdateQuarterAsync(Guid id, UpdateQuarterDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), id);

        entity.Update(dto.OkrReference);
        await _quarterRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Quarter,
            entity.Id,
            entity.Name,
            $"Quarter '{entity.Name}' was updated",
            cancellationToken);

        return MapQuarterToDto(entity);
    }

    public async Task<QuarterDto> ActivateQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), id);

        // Deactivate any currently active quarter
        var activeQuarter = await _quarterRepository.GetActiveAsync(cancellationToken);
        if (activeQuarter != null && activeQuarter.Id != id)
        {
            activeQuarter.ResetToPlanning();
            await _quarterRepository.UpdateAsync(activeQuarter, cancellationToken);
        }

        entity.Activate();
        await _quarterRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Quarter,
            entity.Id,
            entity.Name,
            $"Quarter '{entity.Name}' was activated",
            cancellationToken);

        return MapQuarterToDto(entity);
    }

    public async Task<QuarterDto> CompleteQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), id);

        entity.Complete();
        await _quarterRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Quarter,
            entity.Id,
            entity.Name,
            $"Quarter '{entity.Name}' was marked as completed",
            cancellationToken);

        return MapQuarterToDto(entity);
    }

    public async Task DeleteQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _quarterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), id);

        // Delete all related data
        var initiatives = await _initiativeRepository.GetByQuarterAsync(id, cancellationToken);
        foreach (var initiative in initiatives)
        {
            await _allocationRepository.DeleteByInitiativeAsync(initiative.Id, cancellationToken);
            await _dependencyRepository.DeleteByInitiativeAsync(initiative.Id, cancellationToken);
            await _initiativeMemberRepository.DeleteByInitiativeAsync(initiative.Id, cancellationToken);
            await _initiativeRepository.DeleteAsync(initiative.Id, cancellationToken);
        }

        var sprintGoals = await _sprintGoalRepository.GetByQuarterAsync(id, cancellationToken);
        foreach (var goal in sprintGoals)
        {
            await _sprintGoalRepository.DeleteAsync(goal.Id, cancellationToken);
        }

        await _quarterRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Quarter,
            id,
            entity.Name,
            $"Quarter '{entity.Name}' was deleted",
            cancellationToken);
    }

    #endregion

    #region Initiative Operations

    public async Task<InitiativeDto?> GetInitiativeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _initiativeRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var allocations = await _allocationRepository.GetByInitiativeAsync(id, cancellationToken);
        var members = await _initiativeMemberRepository.GetByInitiativeAsync(id, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var project = entity.ProjectId.HasValue
            ? await _projectRepository.GetByIdAsync(entity.ProjectId.Value, cancellationToken)
            : null;

        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sprintSpan = ComputeSprintSpan(entity.TshirtSize, settings.TshirtSizeMappings);
        var memberDtos = MapMembersToDtos(members, drDict);

        return MapInitiativeToDto(entity, allocations.Count, project?.Name, sprintSpan, memberDtos);
    }

    public async Task<IReadOnlyList<InitiativeDto>> GetInitiativesByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var entities = await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var allocations = await _allocationRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var allMembers = await _initiativeMemberRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var projectDict = projects.ToDictionary(p => p.Id, p => p.Name);
        var settings = await _appSettingsService.GetAsync(cancellationToken);

        return entities.Select(e =>
        {
            var initiativeAllocations = allocations.Where(a => a.InitiativeId == e.Id);
            var projectName = e.ProjectId.HasValue && projectDict.TryGetValue(e.ProjectId.Value, out var name) ? name : null;
            var sprintSpan = ComputeSprintSpan(e.TshirtSize, settings.TshirtSizeMappings);
            var initiativeMembers = allMembers.Where(m => m.InitiativeId == e.Id).ToList();
            var memberDtos = MapMembersToDtos(initiativeMembers, drDict);
            return MapInitiativeToDto(e, initiativeAllocations.Count(), projectName, sprintSpan, memberDtos);
        }).ToList();
    }

    public async Task<InitiativeDto> CreateInitiativeAsync(CreateInitiativeDto dto, CancellationToken cancellationToken = default)
    {
        // Validate quarter exists
        var quarter = await _quarterRepository.GetByIdAsync(dto.QuarterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), dto.QuarterId);

        // Validate project if provided
        string? projectName = null;
        if (dto.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), dto.ProjectId.Value);
            projectName = project.Name;
        }

        // Get existing initiatives to determine unique color
        var existingInitiatives = await _initiativeRepository.GetByQuarterAsync(dto.QuarterId, cancellationToken);
        var usedColors = existingInitiatives.Select(i => i.Color).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var color = GetNextAvailableColor(usedColors);

        var workType = dto.WorkType.HasValue ? (WorkType)dto.WorkType.Value : WorkType.ProductRoadmap;

        var entity = new Initiative(
            dto.QuarterId,
            dto.Name,
            color,
            dto.Description,
            dto.ProjectId,
            dto.TshirtSize,
            dto.Url,
            workType,
            dto.StartSprintId);

        var created = await _initiativeRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Initiative,
            created.Id,
            created.Name,
            $"Initiative '{created.Name}' was created for {quarter.Name}",
            cancellationToken);

        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sprintSpan = ComputeSprintSpan(created.TshirtSize, settings.TshirtSizeMappings);

        return MapInitiativeToDto(created, 0, projectName, sprintSpan, new List<InitiativeMemberDto>());
    }

    private static string GetNextAvailableColor(HashSet<string> usedColors)
    {
        // Find the first available color that's not in use
        foreach (var color in Initiative.AvailableColors)
        {
            if (!usedColors.Contains(color))
            {
                return color;
            }
        }

        // If all colors are used, cycle back (append index to make it slightly different)
        var index = usedColors.Count % Initiative.AvailableColors.Length;
        return Initiative.AvailableColors[index];
    }

    public async Task<InitiativeDto> UpdateInitiativeAsync(Guid id, UpdateInitiativeDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _initiativeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), id);

        // Validate project if provided
        string? projectName = null;
        if (dto.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), dto.ProjectId.Value);
            projectName = project.Name;
        }

        var workType = dto.WorkType.HasValue ? (WorkType)dto.WorkType.Value : (WorkType?)null;

        entity.Update(
            dto.Name,
            dto.Description,
            dto.Color,
            dto.ProjectId,
            dto.TshirtSize,
            dto.Url,
            workType,
            dto.StartSprintId);

        await _initiativeRepository.UpdateAsync(entity, cancellationToken);

        var allocations = await _allocationRepository.GetByInitiativeAsync(id, cancellationToken);
        var members = await _initiativeMemberRepository.GetByInitiativeAsync(id, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Initiative,
            entity.Id,
            entity.Name,
            $"Initiative '{entity.Name}' was updated",
            cancellationToken);

        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sprintSpan = ComputeSprintSpan(entity.TshirtSize, settings.TshirtSizeMappings);
        var memberDtos = MapMembersToDtos(members, drDict);

        return MapInitiativeToDto(entity, allocations.Count, projectName, sprintSpan, memberDtos);
    }

    public async Task DeleteInitiativeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _initiativeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), id);

        await _allocationRepository.DeleteByInitiativeAsync(id, cancellationToken);
        await _dependencyRepository.DeleteByInitiativeAsync(id, cancellationToken);
        await _initiativeMemberRepository.DeleteByInitiativeAsync(id, cancellationToken);
        await _initiativeRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Initiative,
            id,
            entity.Name,
            $"Initiative '{entity.Name}' was deleted",
            cancellationToken);
    }

    #endregion

    #region Allocation Operations

    public async Task<AllocationDto?> GetAllocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _allocationRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var initiative = await _initiativeRepository.GetByIdAsync(entity.InitiativeId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        var sprint = await _sprintRepository.GetByIdAsync(entity.SprintId, cancellationToken);

        return MapAllocationToDto(entity, initiative, directReport, sprint);
    }

    public async Task<IReadOnlyList<AllocationDto>> GetAllocationsByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var allocations = await _allocationRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var initiatives = await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);

        var initiativeDict = initiatives.ToDictionary(i => i.Id);
        var directReportDict = directReports.ToDictionary(d => d.Id);
        var sprintDict = sprints.ToDictionary(s => s.Id);

        return allocations.Select(a =>
        {
            initiativeDict.TryGetValue(a.InitiativeId, out var initiative);
            directReportDict.TryGetValue(a.DirectReportId, out var directReport);
            sprintDict.TryGetValue(a.SprintId, out var sprint);
            return MapAllocationToDto(a, initiative, directReport, sprint);
        }).ToList();
    }

    public async Task<AllocationDto> CreateAllocationAsync(CreateAllocationDto dto, CancellationToken cancellationToken = default)
    {
        // Validate entities exist
        var initiative = await _initiativeRepository.GetByIdAsync(dto.InitiativeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), dto.InitiativeId);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        var sprint = await _sprintRepository.GetByIdAsync(dto.SprintId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sprint), dto.SprintId);

        // Check for existing allocation
        var existing = await _allocationRepository.GetByInitiativeDirectReportSprintAsync(
            dto.InitiativeId, dto.DirectReportId, dto.SprintId, cancellationToken);
        if (existing != null)
        {
            throw new ConflictException($"An allocation already exists for this initiative, team member, and sprint.");
        }

        var entity = new Allocation(
            dto.InitiativeId,
            dto.DirectReportId,
            dto.SprintId);

        var created = await _allocationRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Allocation,
            created.Id,
            $"{directReport.FullName} - {initiative.Name}",
            $"Allocation created: {directReport.FullName} allocated to '{initiative.Name}' in {sprint.Name}",
            cancellationToken);

        return MapAllocationToDto(created, initiative, directReport, sprint);
    }

    
    public async Task DeleteAllocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _allocationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Allocation), id);

        var initiative = await _initiativeRepository.GetByIdAsync(entity.InitiativeId, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _allocationRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Allocation,
            id,
            $"{directReport?.FullName} - {initiative?.Name}",
            $"Allocation deleted",
            cancellationToken);
    }

    #endregion

    #region Sprint Goal Operations

    public async Task<IReadOnlyList<SprintGoalDto>> GetSprintGoalsByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var goals = await _sprintGoalRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        var sprintDict = sprints.ToDictionary(s => s.Id, s => s.Name);

        return goals.Select(g =>
        {
            sprintDict.TryGetValue(g.SprintId, out var sprintName);
            return MapSprintGoalToDto(g, sprintName ?? string.Empty);
        }).ToList();
    }

    public async Task<SprintGoalDto> UpsertSprintGoalAsync(UpsertSprintGoalDto dto, CancellationToken cancellationToken = default)
    {
        // Validate quarter exists
        _ = await _quarterRepository.GetByIdAsync(dto.QuarterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), dto.QuarterId);

        // Validate sprint exists
        var sprint = await _sprintRepository.GetByIdAsync(dto.SprintId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sprint), dto.SprintId);

        var existing = await _sprintGoalRepository.GetByQuarterSprintAsync(dto.QuarterId, dto.SprintId, cancellationToken);

        SprintGoal entity;
        if (existing != null)
        {
            existing.Update(dto.Goal, dto.Notes);
            await _sprintGoalRepository.UpdateAsync(existing, cancellationToken);
            entity = existing;
        }
        else
        {
            entity = new SprintGoal(dto.QuarterId, dto.SprintId, dto.Goal, dto.Notes);
            await _sprintGoalRepository.AddAsync(entity, cancellationToken);
        }

        return MapSprintGoalToDto(entity, sprint.Name);
    }

    #endregion

    #region Dependency Operations

    public async Task<IReadOnlyList<InitiativeDependencyDto>> GetDependenciesByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var dependencies = await _dependencyRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var initiatives = await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var initiativeDict = initiatives.ToDictionary(i => i.Id, i => i.Name);

        return dependencies.Select(d =>
        {
            initiativeDict.TryGetValue(d.DependentInitiativeId, out var dependentName);
            initiativeDict.TryGetValue(d.DependencyInitiativeId, out var dependencyName);
            return MapDependencyToDto(d, dependentName ?? string.Empty, dependencyName ?? string.Empty);
        }).ToList();
    }

    public async Task<InitiativeDependencyDto> CreateDependencyAsync(CreateInitiativeDependencyDto dto, CancellationToken cancellationToken = default)
    {
        // Validate initiatives exist
        var dependent = await _initiativeRepository.GetByIdAsync(dto.DependentInitiativeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), dto.DependentInitiativeId);
        var dependency = await _initiativeRepository.GetByIdAsync(dto.DependencyInitiativeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), dto.DependencyInitiativeId);

        // Check for existing dependency
        if (await _dependencyRepository.ExistsAsync(dto.DependentInitiativeId, dto.DependencyInitiativeId, cancellationToken))
        {
            throw new ConflictException("This dependency relationship already exists.");
        }

        var entity = new InitiativeDependency(
            dto.DependentInitiativeId,
            dto.DependencyInitiativeId,
            dto.Type,
            dto.Notes);

        var created = await _dependencyRepository.AddAsync(entity, cancellationToken);

        return MapDependencyToDto(created, dependent.Name, dependency.Name);
    }

    public async Task DeleteDependencyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dependencyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(InitiativeDependency), id);

        await _dependencyRepository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region Initiative Member Operations

    public async Task<IReadOnlyList<InitiativeMemberDto>> GetInitiativeMembersByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var members = await _initiativeMemberRepository.GetByInitiativeAsync(initiativeId, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);
        return MapMembersToDtos(members, drDict);
    }

    public async Task<InitiativeMemberDto> AddInitiativeMemberAsync(Guid initiativeId, CreateInitiativeMemberDto dto, CancellationToken cancellationToken = default)
    {
        var initiative = await _initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), initiativeId);
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);

        if (await _initiativeMemberRepository.ExistsAsync(initiativeId, dto.DirectReportId, cancellationToken))
        {
            throw new ConflictException($"Member '{directReport.FullName}' is already assigned to initiative '{initiative.Name}'.");
        }

        var entity = new InitiativeMember(initiativeId, dto.DirectReportId);
        var created = await _initiativeMemberRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Initiative,
            initiative.Id,
            initiative.Name,
            $"Member '{directReport.FullName}' was added to initiative '{initiative.Name}'",
            cancellationToken);

        return new InitiativeMemberDto
        {
            Id = created.Id,
            InitiativeId = created.InitiativeId,
            DirectReportId = created.DirectReportId,
            DirectReportName = directReport.FullName,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task RemoveInitiativeMemberAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _initiativeMemberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(InitiativeMember), id);

        await _initiativeMemberRepository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region Sprint Assignment Operations

    public async Task<InitiativeDto> AssignInitiativeToSprintAsync(Guid initiativeId, Guid? startSprintId, CancellationToken cancellationToken = default)
    {
        var entity = await _initiativeRepository.GetByIdAsync(initiativeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Initiative), initiativeId);

        if (startSprintId.HasValue)
        {
            _ = await _sprintRepository.GetByIdAsync(startSprintId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Sprint), startSprintId.Value);
        }

        entity.AssignToSprint(startSprintId);
        await _initiativeRepository.UpdateAsync(entity, cancellationToken);

        var allocations = await _allocationRepository.GetByInitiativeAsync(initiativeId, cancellationToken);
        var members = await _initiativeMemberRepository.GetByInitiativeAsync(initiativeId, cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var project = entity.ProjectId.HasValue
            ? await _projectRepository.GetByIdAsync(entity.ProjectId.Value, cancellationToken)
            : null;

        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sprintSpan = ComputeSprintSpan(entity.TshirtSize, settings.TshirtSizeMappings);
        var memberDtos = MapMembersToDtos(members, drDict);

        return MapInitiativeToDto(entity, allocations.Count, project?.Name, sprintSpan, memberDtos);
    }

    #endregion

    #region Board Operations

    public async Task<PlanningBoardDto> GetPlanningBoardAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarter = await _quarterRepository.GetByIdAsync(quarterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), quarterId);

        // Get sprints for this quarter
        var sprints = await _sprintRepository.GetByYearQuarterAsync(quarter.Year, quarter.QuarterNumber, cancellationToken);

        // Get all data for the quarter
        var initiatives = await GetInitiativesByQuarterAsync(quarterId, cancellationToken);
        var allocations = await GetAllocationsByQuarterAsync(quarterId, cancellationToken);
        var sprintGoals = await GetSprintGoalsByQuarterAsync(quarterId, cancellationToken);
        var dependencies = await GetDependenciesByQuarterAsync(quarterId, cancellationToken);

        // Get team members (only direct reports, not skip-levels)
        var allMembers = await _directReportRepository.GetAllAsync(cancellationToken);
        var directReports = allMembers.Where(d => d.IsDirect).ToList();

        // Get initiative members
        var initiativeMembers = await _initiativeMemberRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var drDict = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var initiativeMemberDtos = MapMembersToDtos(initiativeMembers, drDict);

        // Get leaves that overlap with quarter sprints
        var allLeaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var sprintStartDate = sprints.Min(s => s.StartDate) ?? DateTime.MinValue;
        var sprintEndDate = sprints.Max(s => s.EndDate) ?? DateTime.MaxValue;
        var relevantLeaves = allLeaves
            .Where(l => l.StartDate <= sprintEndDate && l.EndDate >= sprintStartDate)
            .ToList();

        return new PlanningBoardDto
        {
            Quarter = MapQuarterToDto(quarter),
            Initiatives = initiatives,
            Sprints = sprints.Select(s => new SprintDto
            {
                Id = s.Id,
                Name = s.Name,
                TeamName = s.TeamName,
                Quarter = s.Quarter,
                Year = s.Year,
                SprintNumber = s.SprintNumber,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            TeamMembers = directReports.Select(d => new DirectReportDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                JobTitle = d.JobTitle,
                Department = d.Department,
                HireDate = d.HireDate,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                FullName = d.FullName
            }).ToList(),
            Allocations = allocations,
            SprintGoals = sprintGoals,
            Dependencies = dependencies,
            InitiativeMembers = initiativeMemberDtos,
            Leaves = relevantLeaves.Select(l => new LeaveDto
            {
                Id = l.Id,
                DirectReportId = l.DirectReportId,
                Type = l.Type.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                DaysCount = l.DaysCount,
                BusinessDaysCount = l.BusinessDaysCount,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList()
        };
    }

    #endregion

    #region Mapping Methods

    private static QuarterDto MapQuarterToDto(Quarter entity) => new()
    {
        Id = entity.Id,
        Year = entity.Year,
        QuarterNumber = entity.QuarterNumber,
        Name = entity.Name,
        Status = entity.Status,
        OkrReference = entity.OkrReference,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static InitiativeDto MapInitiativeToDto(
        Initiative entity,
        int allocationCount,
        string? projectName,
        int sprintSpan = 1,
        IReadOnlyList<InitiativeMemberDto>? members = null) => new()
    {
        Id = entity.Id,
        QuarterId = entity.QuarterId,
        Name = entity.Name,
        Description = entity.Description,
        Color = entity.Color,
        ProjectId = entity.ProjectId,
        ProjectName = projectName,
        TshirtSize = entity.TshirtSize,
        Url = entity.Url,
        WorkType = (int)entity.WorkType,
        StartSprintId = entity.StartSprintId,
        SprintSpan = sprintSpan,
        Members = members ?? new List<InitiativeMemberDto>(),
        AllocationCount = allocationCount,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static int ComputeSprintSpan(string tshirtSize, List<TshirtSizeMapping> mappings)
    {
        var mapping = mappings.FirstOrDefault(m =>
            string.Equals(m.Size, tshirtSize, StringComparison.OrdinalIgnoreCase));
        if (mapping != null)
        {
            return (int)Math.Ceiling(mapping.Sprints);
        }
        // Default: S=1, M=1, L=2, XL=4
        return tshirtSize.ToUpperInvariant() switch
        {
            "S" => 1,
            "M" => 1,
            "L" => 2,
            "XL" => 4,
            _ => 1
        };
    }

    private static List<InitiativeMemberDto> MapMembersToDtos(
        IEnumerable<InitiativeMember> members,
        Dictionary<Guid, string> drDict)
    {
        return members.Select(m => new InitiativeMemberDto
        {
            Id = m.Id,
            InitiativeId = m.InitiativeId,
            DirectReportId = m.DirectReportId,
            DirectReportName = drDict.TryGetValue(m.DirectReportId, out var name) ? name : string.Empty,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    private static AllocationDto MapAllocationToDto(
        Allocation entity,
        Initiative? initiative,
        DirectReport? directReport,
        Sprint? sprint) => new()
    {
        Id = entity.Id,
        InitiativeId = entity.InitiativeId,
        InitiativeName = initiative?.Name ?? string.Empty,
        InitiativeColor = initiative?.Color ?? string.Empty,
        DirectReportId = entity.DirectReportId,
        DirectReportName = directReport?.FullName ?? string.Empty,
        SprintId = entity.SprintId,
        SprintName = sprint?.Name ?? string.Empty,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static SprintGoalDto MapSprintGoalToDto(SprintGoal entity, string sprintName) => new()
    {
        Id = entity.Id,
        QuarterId = entity.QuarterId,
        SprintId = entity.SprintId,
        SprintName = sprintName,
        Goal = entity.Goal,
        Notes = entity.Notes,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static InitiativeDependencyDto MapDependencyToDto(
        InitiativeDependency entity,
        string dependentName,
        string dependencyName) => new()
    {
        Id = entity.Id,
        DependentInitiativeId = entity.DependentInitiativeId,
        DependentInitiativeName = dependentName,
        DependencyInitiativeId = entity.DependencyInitiativeId,
        DependencyInitiativeName = dependencyName,
        Type = entity.Type,
        Notes = entity.Notes,
        CreatedAt = entity.CreatedAt
    };

    #endregion

    #region Export Operations

    public async Task<byte[]> ExportPlanningBoardToExcelAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var board = await GetPlanningBoardAsync(quarterId, cancellationToken);

        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using var package = new OfficeOpenXml.ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add($"{board.Quarter.Name} Plan-Catalogue");

        // Sort sprints by their order
        var sortedSprints = board.Sprints.OrderBy(s => s.Name).ToList();

        // Header Row 1: Sprint columns
        worksheet.Cells[1, 1].Value = "Sprint";
        for (int i = 0; i < sortedSprints.Count; i++)
        {
            worksheet.Cells[1, i + 2].Value = sortedSprints[i].Name;
        }

        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, sortedSprints.Count + 1])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
        }

        int currentRow = 3;

        // Options Section
        currentRow = AddOptionsSection(worksheet, board, sortedSprints, currentRow);

        // Delivery Plan Section
        currentRow = AddDeliveryPlanSection(worksheet, board, sortedSprints, currentRow + 2);

        // Sprint Goals Section
        currentRow = AddSprintGoalsSection(worksheet, board, sortedSprints, currentRow + 2);

        // Auto-fit columns
        for (int col = 1; col <= sortedSprints.Count + 1; col++)
        {
            worksheet.Column(col).Width = 30;
        }

        // Row height for better readability
        worksheet.Row(1).Height = 30;

        return await Task.FromResult(package.GetAsByteArray());
    }

    private int AddDeliveryPlanSection(
        OfficeOpenXml.ExcelWorksheet worksheet,
        PlanningBoardDto board,
        List<SprintDto> sortedSprints,
        int startRow)
    {
        int currentRow = startRow;

        // Section header
        worksheet.Cells[currentRow, 1].Value = "Delivery Plan";
        using (var range = worksheet.Cells[currentRow, 1, currentRow, sortedSprints.Count + 1])
        {
            range.Merge = true;
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 14;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
        }
        currentRow++;

        // Group allocations by team member
        var allocationsByMember = board.Allocations
            .GroupBy(a => a.DirectReportId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Add team member rows
        foreach (var member in board.TeamMembers.OrderBy(m => m.FullName))
        {
            worksheet.Cells[currentRow, 1].Value = member.FullName;
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;

            // For each sprint, show allocated initiatives
            for (int i = 0; i < sortedSprints.Count; i++)
            {
                var sprint = sortedSprints[i];
                var memberAllocations = allocationsByMember.ContainsKey(member.Id)
                    ? allocationsByMember[member.Id].Where(a => a.SprintId == sprint.Id).ToList()
                    : new List<AllocationDto>();

                if (memberAllocations.Any())
                {
                    var initiativeNames = memberAllocations
                        .Select(a => board.Initiatives.FirstOrDefault(init => init.Id == a.InitiativeId))
                        .Where(init => init != null)
                        .Select(init => init!.Name)
                        .Distinct();

                    var cell = worksheet.Cells[currentRow, i + 2];
                    cell.Value = string.Join("\n", initiativeNames);
                    cell.Style.WrapText = true;

                    // Apply color from first initiative
                    var firstInitiative = board.Initiatives.FirstOrDefault(init => init.Id == memberAllocations[0].InitiativeId);
                    if (firstInitiative != null && !string.IsNullOrEmpty(firstInitiative.Color))
                    {
                        var color = ColorTranslator.FromHtml(firstInitiative.Color);
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(color);

                        // Adjust font color for readability
                        if (IsColorDark(color))
                        {
                            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        }
                    }
                }
                else
                {
                    worksheet.Cells[currentRow, i + 2].Value = "";
                }

                // Add borders
                worksheet.Cells[currentRow, i + 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Border for team member name cell
            worksheet.Cells[currentRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            currentRow++;
        }

        return currentRow;
    }

    private int AddSprintGoalsSection(
        OfficeOpenXml.ExcelWorksheet worksheet,
        PlanningBoardDto board,
        List<SprintDto> sortedSprints,
        int startRow)
    {
        int currentRow = startRow;

        // Section header
        worksheet.Cells[currentRow, 1].Value = "Sprint Goals Prioritized (Describing the Increments)";
        using (var range = worksheet.Cells[currentRow, 1, currentRow, sortedSprints.Count + 1])
        {
            range.Merge = true;
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 14;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium);
        }
        currentRow++;

        // Add sprint goals
        for (int i = 0; i < sortedSprints.Count; i++)
        {
            var sprint = sortedSprints[i];
            var goal = board.SprintGoals.FirstOrDefault(g => g.SprintId == sprint.Id);

            var cell = worksheet.Cells[currentRow, i + 2];
            cell.Value = goal?.Goal ?? "";
            cell.Style.WrapText = true;
            cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        }

        worksheet.Cells[currentRow, 1].Value = "Goals";
        worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
        worksheet.Cells[currentRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        worksheet.Row(currentRow).Height = 100;

        currentRow++;

        return currentRow;
    }

    private int AddOptionsSection(
        OfficeOpenXml.ExcelWorksheet worksheet,
        PlanningBoardDto board,
        List<SprintDto> sortedSprints,
        int startRow)
    {
        int currentRow = startRow;

        // Section header row
        worksheet.Cells[currentRow, 1].Value = "Options";
        worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
        worksheet.Cells[currentRow, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        worksheet.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 224, 240));
        worksheet.Cells[currentRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

        // Add initiatives horizontally across columns (no limit)
        var initiatives = board.Initiatives.OrderBy(i => i.Name).ToList();
        int colIndex = 2;

        foreach (var initiative in initiatives)
        {
            var cell = worksheet.Cells[currentRow, colIndex];

            if (!string.IsNullOrWhiteSpace(initiative.Url))
            {
                // Add as hyperlink
                cell.Hyperlink = new Uri(initiative.Url);
                cell.Value = initiative.Name;
                cell.Style.Font.UnderLine = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.Blue);
            }
            else
            {
                // Add as plain text
                cell.Value = initiative.Name;
            }

            cell.Style.WrapText = true;
            cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

            colIndex++;
        }

        currentRow++;
        return currentRow;
    }

    private static bool IsColorDark(System.Drawing.Color color)
    {
        // Calculate perceived brightness
        double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
        return brightness < 0.5;
    }

    #endregion
}
