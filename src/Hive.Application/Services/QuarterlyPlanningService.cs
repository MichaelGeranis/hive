using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Changes to a quarter's plan. Every rule about a single entity lives on the entity; this
/// service checks what spans several: that things belong to the same quarter, one lead per
/// initiative, a person's sprint never beyond 100%.
/// </summary>
public class QuarterlyPlanningService : IQuarterlyPlanningService
{
    private readonly IQuarterRepository _quarterRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly ISprintGoalRepository _sprintGoalRepository;
    private readonly IInitiativeDependencyRepository _dependencyRepository;
    private readonly IInitiativeMemberRepository _memberRepository;
    private readonly IExternalDependencyRepository _externalDependencyRepository;
    private readonly IQuarterMemberRepository _quarterMemberRepository;
    private readonly IInitiativeProductRepository _initiativeProductRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProductRepository _productRepository;
    private readonly IActivityService _activityService;

    public QuarterlyPlanningService(
        IQuarterRepository quarterRepository,
        IInitiativeRepository initiativeRepository,
        IAllocationRepository allocationRepository,
        ISprintGoalRepository sprintGoalRepository,
        IInitiativeDependencyRepository dependencyRepository,
        IInitiativeMemberRepository memberRepository,
        IExternalDependencyRepository externalDependencyRepository,
        IQuarterMemberRepository quarterMemberRepository,
        IInitiativeProductRepository initiativeProductRepository,
        ISprintRepository sprintRepository,
        IDirectReportRepository directReportRepository,
        IProductRepository productRepository,
        IActivityService activityService)
    {
        _quarterRepository = quarterRepository ?? throw new ArgumentNullException(nameof(quarterRepository));
        _initiativeRepository = initiativeRepository ?? throw new ArgumentNullException(nameof(initiativeRepository));
        _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
        _sprintGoalRepository = sprintGoalRepository ?? throw new ArgumentNullException(nameof(sprintGoalRepository));
        _dependencyRepository = dependencyRepository ?? throw new ArgumentNullException(nameof(dependencyRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _externalDependencyRepository = externalDependencyRepository ?? throw new ArgumentNullException(nameof(externalDependencyRepository));
        _quarterMemberRepository = quarterMemberRepository ?? throw new ArgumentNullException(nameof(quarterMemberRepository));
        _initiativeProductRepository = initiativeProductRepository ?? throw new ArgumentNullException(nameof(initiativeProductRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    #region Quarters

    public async Task<IReadOnlyList<QuarterDto>> GetAllQuartersAsync(CancellationToken cancellationToken = default)
    {
        var quarters = await _quarterRepository.GetAllAsync(cancellationToken);
        return quarters
            .OrderByDescending(q => q.Year)
            .ThenByDescending(q => q.QuarterNumber)
            .Select(MapQuarter)
            .ToList();
    }

    public async Task<QuarterDto?> GetQuarterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quarter = await _quarterRepository.GetByIdAsync(id, cancellationToken);
        return quarter is null ? null : MapQuarter(quarter);
    }

    /// <summary>
    /// Creates a quarter with its two standing lanes, Maintenance and the Tech roadmap.
    /// </summary>
    public async Task<QuarterDto> CreateQuarterAsync(CreateQuarterDto dto, CancellationToken cancellationToken = default)
    {
        var quarter = await CreateQuarterWithLanesAsync(dto.Year, dto.QuarterNumber, dto.OkrReference, cancellationToken);
        return MapQuarter(quarter);
    }

    public async Task<QuarterDto> UpdateQuarterAsync(Guid id, UpdateQuarterDto dto, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(id, cancellationToken);

        quarter.Update(dto.OkrReference);
        if (dto.ProductRoadmapPercent.HasValue || dto.TechRoadmapPercent.HasValue || dto.MaintenancePercent.HasValue)
        {
            quarter.SetMixTarget(
                dto.ProductRoadmapPercent ?? quarter.ProductRoadmapPercent,
                dto.TechRoadmapPercent ?? quarter.TechRoadmapPercent,
                dto.MaintenancePercent ?? quarter.MaintenancePercent);
        }

        await _quarterRepository.UpdateAsync(quarter, cancellationToken);
        return MapQuarter(quarter);
    }

    public async Task<QuarterDto> ActivateQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(id, cancellationToken);

        // One quarter is active at a time
        var active = await _quarterRepository.GetActiveAsync(cancellationToken);
        if (active != null && active.Id != id)
        {
            active.ResetToPlanning();
            await _quarterRepository.UpdateAsync(active, cancellationToken);
        }

        quarter.Activate();
        await _quarterRepository.UpdateAsync(quarter, cancellationToken);
        await LogAsync(ActivityType.StatusChanged, EntityType.Quarter, quarter.Id, quarter.Name, $"Quarter '{quarter.Name}' was activated", cancellationToken);
        return MapQuarter(quarter);
    }

    public async Task<QuarterDto> CompleteQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(id, cancellationToken);
        quarter.Complete();
        await _quarterRepository.UpdateAsync(quarter, cancellationToken);
        await LogAsync(ActivityType.Completed, EntityType.Quarter, quarter.Id, quarter.Name, $"Quarter '{quarter.Name}' was completed", cancellationToken);
        return MapQuarter(quarter);
    }

    public async Task DeleteQuarterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(id, cancellationToken);

        foreach (var initiative in await _initiativeRepository.GetByQuarterAsync(id, cancellationToken))
        {
            await DeleteInitiativeDataAsync(initiative.Id, cancellationToken);
        }

        foreach (var goal in await _sprintGoalRepository.GetByQuarterAsync(id, cancellationToken))
        {
            await _sprintGoalRepository.DeleteAsync(goal.Id, cancellationToken);
        }

        await _quarterMemberRepository.DeleteByQuarterAsync(id, cancellationToken);
        await _quarterRepository.DeleteAsync(id, cancellationToken);
        await LogAsync(ActivityType.Deleted, EntityType.Quarter, id, quarter.Name, $"Quarter '{quarter.Name}' was deleted", cancellationToken);
    }

    #endregion

    #region Initiatives

    public async Task<Guid> CreateInitiativeAsync(CreateInitiativeDto dto, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(dto.QuarterId, cancellationToken);
        var existing = await _initiativeRepository.GetByQuarterAsync(dto.QuarterId, cancellationToken);

        var kind = dto.Kind.HasValue ? (InitiativeKind)dto.Kind.Value : InitiativeKind.Option;
        if (kind == InitiativeKind.Bucket)
        {
            throw new ArgumentException("Standing lanes are added as lanes, not as options.", nameof(dto));
        }

        var initiative = new Initiative(
            dto.QuarterId,
            dto.Name,
            NextColor(existing),
            dto.Description,
            dto.TshirtSize,
            dto.Url,
            dto.WorkType.HasValue ? (WorkType)dto.WorkType.Value : WorkType.ProductRoadmap,
            kind,
            NextRank(existing),
            dto.ArrivedOn,
            dto.DueDate,
            dto.Status.HasValue ? (InitiativeStatus)dto.Status.Value : InitiativeStatus.InDiscovery);

        await _initiativeRepository.AddAsync(initiative, cancellationToken);
        await LogAsync(ActivityType.Created, EntityType.Initiative, initiative.Id, initiative.Name,
            $"Option '{initiative.Name}' was added to {quarter.Name}", cancellationToken);
        return initiative.Id;
    }

    public async Task<Guid> AddBucketAsync(Guid quarterId, AddBucketDto dto, CancellationToken cancellationToken = default)
    {
        await GetQuarterAsync(quarterId, cancellationToken);
        var workType = (WorkType)dto.WorkType;
        var existing = await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken);
        if (existing.Any(i => i.IsBucket && i.WorkType == workType))
        {
            throw new ConflictException("This quarter already has that lane.");
        }

        var lane = Initiative.CreateBucket(quarterId, workType);
        await _initiativeRepository.AddAsync(lane, cancellationToken);
        return lane.Id;
    }

    public async Task UpdateInitiativeAsync(Guid id, UpdateInitiativeDto dto, CancellationToken cancellationToken = default)
    {
        var initiative = await GetInitiativeAsync(id, cancellationToken);

        if (dto.EarliestSprintId.HasValue)
        {
            await GetQuarterSprintAsync(initiative.QuarterId, dto.EarliestSprintId.Value, cancellationToken);
        }

        initiative.Update(
            dto.Name,
            dto.Description,
            dto.Color,
            dto.Url,
            dto.WorkType.HasValue ? (WorkType)dto.WorkType.Value : null,
            dto.Kind.HasValue ? (InitiativeKind)dto.Kind.Value : null,
            dto.ArrivedOn,
            dto.DueDate);
        if (!initiative.IsBucket) initiative.SetSize(dto.TshirtSize);
        initiative.SetReadiness((ReadinessIssue)dto.Readiness);
        initiative.SetEarliestSprint(dto.EarliestSprintId);
        await _initiativeRepository.UpdateAsync(initiative, cancellationToken);

        if (dto.ProductIds is not null)
        {
            var wanted = dto.ProductIds.Distinct().ToList();
            foreach (var productId in wanted)
            {
                _ = await _productRepository.GetByIdAsync(productId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Product), productId);
            }

            await _initiativeProductRepository.DeleteByInitiativeAsync(id, cancellationToken);
            foreach (var productId in wanted)
            {
                await _initiativeProductRepository.AddAsync(new InitiativeProduct(id, productId), cancellationToken);
            }
        }
    }

    public async Task ChangeInitiativeStatusAsync(Guid id, ChangeInitiativeStatusDto dto, CancellationToken cancellationToken = default)
    {
        var initiative = await GetInitiativeAsync(id, cancellationToken);

        switch ((InitiativeStatus)dto.Status)
        {
            case InitiativeStatus.InDiscovery: initiative.MarkInDiscovery(); break;
            case InitiativeStatus.InSolutionDesign: initiative.MarkInSolutionDesign(); break;
            case InitiativeStatus.ReadyForCommitment: initiative.MarkReadyForCommitment(); break;
            case InitiativeStatus.Committed: initiative.Commit(dto.Note); break;
            case InitiativeStatus.Stretch: initiative.MakeStretch(dto.Note); break;
            case InitiativeStatus.Dropped: initiative.Drop(dto.Note); break;
            case InitiativeStatus.Delivered: initiative.MarkDelivered(); break;
            default: throw new ArgumentException("Unknown status.", nameof(dto));
        }

        await _initiativeRepository.UpdateAsync(initiative, cancellationToken);
        await LogAsync(ActivityType.StatusChanged, EntityType.Initiative, initiative.Id, initiative.Name,
            $"'{initiative.Name}' is now {initiative.Status}", cancellationToken);
    }

    /// <summary>
    /// Sets the priority order: the listed options get ranks 1, 2, 3… in that order.
    /// </summary>
    public async Task ReorderInitiativesAsync(Guid quarterId, ReorderInitiativesDto dto, CancellationToken cancellationToken = default)
    {
        await GetQuarterAsync(quarterId, cancellationToken);
        var initiatives = (await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken)).ToDictionary(i => i.Id);

        var rank = 1;
        foreach (var id in dto.OrderedIds.Distinct())
        {
            if (!initiatives.TryGetValue(id, out var initiative))
            {
                throw new NotFoundException(nameof(Initiative), id);
            }

            if (initiative.IsBucket) continue;
            if (initiative.Rank != rank)
            {
                initiative.SetRank(rank);
                await _initiativeRepository.UpdateAsync(initiative, cancellationToken);
            }

            rank++;
        }
    }

    /// <summary>
    /// Copies an option to the next quarter as a leftover, with the dependencies not yet
    /// done, its products and its people. The next quarter
    /// is created when it does not exist yet.
    /// </summary>
    public async Task<Guid> CarryOverInitiativeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var initiative = await GetInitiativeAsync(id, cancellationToken);
        var quarter = await GetQuarterAsync(initiative.QuarterId, cancellationToken);
        var (year, number) = quarter.Next();
        var next = await _quarterRepository.GetByYearQuarterAsync(year, number, cancellationToken)
                   ?? await CreateQuarterWithLanesAsync(year, number, null, cancellationToken);

        var copy = initiative.CarryOverTo(next.Id);
        await _initiativeRepository.AddAsync(copy, cancellationToken);

        foreach (var dependency in (await _externalDependencyRepository.GetByInitiativeAsync(id, cancellationToken))
                     .Where(d => d.Status != ExternalDependencyStatus.Done))
        {
            await _externalDependencyRepository.AddAsync(new ExternalDependency(copy.Id, dependency.Team, dependency.Need, dependency.Status), cancellationToken);
        }

        foreach (var product in await _initiativeProductRepository.GetByInitiativeAsync(id, cancellationToken))
        {
            await _initiativeProductRepository.AddAsync(new InitiativeProduct(copy.Id, product.ProductId), cancellationToken);
        }

        foreach (var member in await _memberRepository.GetByInitiativeAsync(id, cancellationToken))
        {
            await _memberRepository.AddAsync(new InitiativeMember(copy.Id, member.DirectReportId, member.Role), cancellationToken);
        }

        await LogAsync(ActivityType.Created, EntityType.Initiative, copy.Id, copy.Name,
            $"'{copy.Name}' was carried over from {quarter.Name} to {next.Name}", cancellationToken);
        return copy.Id;
    }

    public async Task DeleteInitiativeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var initiative = await GetInitiativeAsync(id, cancellationToken);
        await DeleteInitiativeDataAsync(id, cancellationToken);
        await LogAsync(ActivityType.Deleted, EntityType.Initiative, id, initiative.Name, $"'{initiative.Name}' was deleted", cancellationToken);
    }

    #endregion

    #region People on initiatives

    public async Task<Guid> AddInitiativeMemberAsync(Guid initiativeId, CreateInitiativeMemberDto dto, CancellationToken cancellationToken = default)
    {
        var initiative = await GetInitiativeAsync(initiativeId, cancellationToken);
        var person = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken)
                     ?? throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);

        if (await _memberRepository.ExistsAsync(initiativeId, dto.DirectReportId, cancellationToken))
        {
            throw new ConflictException($"{person.FullName} is already on '{initiative.Name}'.");
        }

        var member = new InitiativeMember(initiativeId, dto.DirectReportId, (InitiativeRole)dto.Role);
        if (member.IsLead)
        {
            await DemoteCurrentLeadAsync(initiativeId, cancellationToken);
        }

        await _memberRepository.AddAsync(member, cancellationToken);
        return member.Id;
    }

    /// <summary>
    /// Changes a member's role. An initiative has one lead: naming a new one makes the
    /// previous lead a contributor.
    /// </summary>
    public async Task UpdateInitiativeMemberAsync(Guid memberId, UpdateInitiativeMemberDto dto, CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
                     ?? throw new NotFoundException(nameof(InitiativeMember), memberId);

        var role = (InitiativeRole)dto.Role;
        if (role == InitiativeRole.Lead && !member.IsLead)
        {
            await DemoteCurrentLeadAsync(member.InitiativeId, cancellationToken);
        }

        member.SetRole(role);
        await _memberRepository.UpdateAsync(member, cancellationToken);
    }

    public async Task RemoveInitiativeMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        _ = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new NotFoundException(nameof(InitiativeMember), memberId);
        await _memberRepository.DeleteAsync(memberId, cancellationToken);
    }

    private async Task DemoteCurrentLeadAsync(Guid initiativeId, CancellationToken cancellationToken)
    {
        foreach (var lead in (await _memberRepository.GetByInitiativeAsync(initiativeId, cancellationToken)).Where(m => m.IsLead))
        {
            lead.SetRole(InitiativeRole.Contributor);
            await _memberRepository.UpdateAsync(lead, cancellationToken);
        }
    }

    #endregion

    #region Dependencies

    public async Task<Guid> AddExternalDependencyAsync(Guid initiativeId, SaveExternalDependencyDto dto, CancellationToken cancellationToken = default)
    {
        await GetInitiativeAsync(initiativeId, cancellationToken);
        var dependency = new ExternalDependency(initiativeId, dto.Team, dto.Need, (ExternalDependencyStatus)dto.Status);
        await _externalDependencyRepository.AddAsync(dependency, cancellationToken);
        return dependency.Id;
    }

    public async Task UpdateExternalDependencyAsync(Guid id, SaveExternalDependencyDto dto, CancellationToken cancellationToken = default)
    {
        var dependency = await _externalDependencyRepository.GetByIdAsync(id, cancellationToken)
                         ?? throw new NotFoundException(nameof(ExternalDependency), id);
        dependency.Update(dto.Team, dto.Need, (ExternalDependencyStatus)dto.Status);
        await _externalDependencyRepository.UpdateAsync(dependency, cancellationToken);
    }

    public async Task DeleteExternalDependencyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = await _externalDependencyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ExternalDependency), id);
        await _externalDependencyRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<Guid> CreateDependencyAsync(CreateInitiativeDependencyDto dto, CancellationToken cancellationToken = default)
    {
        var dependent = await GetInitiativeAsync(dto.DependentInitiativeId, cancellationToken);
        var needed = await GetInitiativeAsync(dto.DependencyInitiativeId, cancellationToken);
        if (dependent.QuarterId != needed.QuarterId)
        {
            throw new ArgumentException("Both initiatives must be in the same quarter.", nameof(dto));
        }

        if (await _dependencyRepository.ExistsAsync(dto.DependentInitiativeId, dto.DependencyInitiativeId, cancellationToken))
        {
            throw new ConflictException("This dependency already exists.");
        }

        var dependency = new InitiativeDependency(dto.DependentInitiativeId, dto.DependencyInitiativeId, dto.Type, dto.Notes);
        await _dependencyRepository.AddAsync(dependency, cancellationToken);
        return dependency.Id;
    }

    public async Task DeleteDependencyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = await _dependencyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(InitiativeDependency), id);
        await _dependencyRepository.DeleteAsync(id, cancellationToken);
    }

    #endregion

    #region Roster, grid, sprint notes

    /// <summary>
    /// Puts a person on the quarter's plan, or changes their planned capacity. The first change
    /// turns the default roster (every direct report) into the quarter's own.
    /// </summary>
    public async Task SetRosterMemberAsync(Guid quarterId, SetRosterMemberDto dto, CancellationToken cancellationToken = default)
    {
        await GetQuarterAsync(quarterId, cancellationToken);
        _ = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);

        await MaterialiseRosterAsync(quarterId, cancellationToken);

        var existing = await _quarterMemberRepository.GetByQuarterAndDirectReportAsync(quarterId, dto.DirectReportId, cancellationToken);
        if (existing is null)
        {
            await _quarterMemberRepository.AddAsync(new QuarterMember(quarterId, dto.DirectReportId, dto.CapacityPercent), cancellationToken);
        }
        else
        {
            existing.SetCapacity(dto.CapacityPercent);
            await _quarterMemberRepository.UpdateAsync(existing, cancellationToken);
        }
    }

    /// <summary>
    /// Takes a person off the quarter's plan, and their cells with them.
    /// </summary>
    public async Task RemoveRosterMemberAsync(Guid quarterId, Guid directReportId, CancellationToken cancellationToken = default)
    {
        await GetQuarterAsync(quarterId, cancellationToken);
        await MaterialiseRosterAsync(quarterId, cancellationToken);

        var member = await _quarterMemberRepository.GetByQuarterAndDirectReportAsync(quarterId, directReportId, cancellationToken)
                     ?? throw new NotFoundException(nameof(QuarterMember), directReportId);

        foreach (var allocation in (await _allocationRepository.GetByQuarterAsync(quarterId, cancellationToken))
                     .Where(a => a.DirectReportId == directReportId))
        {
            await _allocationRepository.DeleteAsync(allocation.Id, cancellationToken);
        }

        await _quarterMemberRepository.DeleteAsync(member.Id, cancellationToken);
    }

    /// <summary>
    /// Replaces whole cells of the grid: each person's sprint gets exactly the shares given.
    /// All cells are checked before any is written.
    /// </summary>
    public async Task SetCellsAsync(Guid quarterId, SetCellsDto dto, CancellationToken cancellationToken = default)
    {
        var quarter = await GetQuarterAsync(quarterId, cancellationToken);
        var sprints = (await _sprintRepository.GetByYearQuarterAsync(quarter.Year, quarter.QuarterNumber, cancellationToken))
            .Select(s => s.Id).ToHashSet();
        var initiatives = (await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken)).ToDictionary(i => i.Id);
        var people = (await _directReportRepository.GetAllAsync(cancellationToken)).Select(p => p.Id).ToHashSet();

        foreach (var cell in dto.Cells)
        {
            if (!people.Contains(cell.DirectReportId)) throw new NotFoundException(nameof(DirectReport), cell.DirectReportId);
            if (!sprints.Contains(cell.SprintId)) throw new NotFoundException(nameof(Sprint), cell.SprintId);
            foreach (var share in cell.Shares)
            {
                if (!initiatives.ContainsKey(share.InitiativeId)) throw new NotFoundException(nameof(Initiative), share.InitiativeId);
            }

            Allocation.ValidateCell(cell.Shares.Select(s => (s.InitiativeId, s.Percent)).ToList());
        }

        var allocations = await _allocationRepository.GetByQuarterAsync(quarterId, cancellationToken);
        foreach (var cell in dto.Cells)
        {
            var current = allocations.Where(a => a.DirectReportId == cell.DirectReportId && a.SprintId == cell.SprintId).ToList();
            foreach (var allocation in current.Where(a => cell.Shares.All(s => s.InitiativeId != a.InitiativeId)))
            {
                await _allocationRepository.DeleteAsync(allocation.Id, cancellationToken);
            }

            foreach (var share in cell.Shares)
            {
                var allocation = current.FirstOrDefault(a => a.InitiativeId == share.InitiativeId);
                if (allocation is null)
                {
                    await _allocationRepository.AddAsync(new Allocation(share.InitiativeId, cell.DirectReportId, cell.SprintId, share.Percent), cancellationToken);
                }
                else if (allocation.Percent != share.Percent)
                {
                    allocation.SetPercent(share.Percent);
                    await _allocationRepository.UpdateAsync(allocation, cancellationToken);
                }
            }
        }
    }

    public async Task SetSprintNotesAsync(Guid quarterId, Guid sprintId, SprintNotesDto dto, CancellationToken cancellationToken = default)
    {
        await GetQuarterSprintAsync(quarterId, sprintId, cancellationToken);

        var existing = await _sprintGoalRepository.GetByQuarterSprintAsync(quarterId, sprintId, cancellationToken);
        if (existing is null)
        {
            await _sprintGoalRepository.AddAsync(new SprintGoal(quarterId, sprintId, null, dto.Notes), cancellationToken);
        }
        else
        {
            existing.Update(existing.Goal, dto.Notes);
            await _sprintGoalRepository.UpdateAsync(existing, cancellationToken);
        }
    }

    private async Task MaterialiseRosterAsync(Guid quarterId, CancellationToken cancellationToken)
    {
        if ((await _quarterMemberRepository.GetByQuarterAsync(quarterId, cancellationToken)).Count > 0) return;

        var people = await _directReportRepository.GetAllAsync(cancellationToken);
        foreach (var entry in QuarterPlan.ResolveRoster(Array.Empty<QuarterMember>(), people))
        {
            await _quarterMemberRepository.AddAsync(new QuarterMember(quarterId, entry.Person.Id, entry.CapacityPercent), cancellationToken);
        }
    }

    #endregion

    #region Helpers

    private async Task<Quarter> CreateQuarterWithLanesAsync(int year, int quarterNumber, string? okrReference, CancellationToken cancellationToken)
    {
        if (await _quarterRepository.ExistsAsync(year, quarterNumber, cancellationToken))
        {
            throw new ConflictException($"Quarter Q{quarterNumber} {year} already exists.");
        }

        var quarter = new Quarter(year, quarterNumber, okrReference);
        await _quarterRepository.AddAsync(quarter, cancellationToken);
        await _initiativeRepository.AddAsync(Initiative.CreateBucket(quarter.Id, WorkType.Maintenance), cancellationToken);
        await _initiativeRepository.AddAsync(Initiative.CreateBucket(quarter.Id, WorkType.TechRoadmap), cancellationToken);

        await LogAsync(ActivityType.Created, EntityType.Quarter, quarter.Id, quarter.Name, $"Quarter '{quarter.Name}' was created", cancellationToken);
        return quarter;
    }

    private async Task DeleteInitiativeDataAsync(Guid initiativeId, CancellationToken cancellationToken)
    {
        await _allocationRepository.DeleteByInitiativeAsync(initiativeId, cancellationToken);
        await _dependencyRepository.DeleteByInitiativeAsync(initiativeId, cancellationToken);
        await _memberRepository.DeleteByInitiativeAsync(initiativeId, cancellationToken);
        await _externalDependencyRepository.DeleteByInitiativeAsync(initiativeId, cancellationToken);
        await _initiativeProductRepository.DeleteByInitiativeAsync(initiativeId, cancellationToken);
        await _initiativeRepository.DeleteAsync(initiativeId, cancellationToken);
    }

    private async Task<Quarter> GetQuarterAsync(Guid id, CancellationToken cancellationToken) =>
        await _quarterRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Quarter), id);

    private async Task<Initiative> GetInitiativeAsync(Guid id, CancellationToken cancellationToken) =>
        await _initiativeRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Initiative), id);

    /// <summary>A sprint that belongs to the quarter: same year and quarter number.</summary>
    private async Task<Sprint> GetQuarterSprintAsync(Guid quarterId, Guid sprintId, CancellationToken cancellationToken)
    {
        var quarter = await GetQuarterAsync(quarterId, cancellationToken);
        var sprint = await _sprintRepository.GetByIdAsync(sprintId, cancellationToken)
                     ?? throw new NotFoundException(nameof(Sprint), sprintId);
        if (sprint.Year != quarter.Year || sprint.Quarter != quarter.QuarterNumber)
        {
            throw new ArgumentException($"{sprint.Name} is not a sprint of {quarter.Name}.", nameof(sprintId));
        }

        return sprint;
    }

    private static int NextRank(IReadOnlyList<Initiative> existing) =>
        existing.Where(i => !i.IsBucket).Select(i => i.Rank).DefaultIfEmpty(0).Max() + 1;

    private static string NextColor(IReadOnlyList<Initiative> existing)
    {
        var used = existing.Select(i => i.Color).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Initiative.AvailableColors.FirstOrDefault(c => !used.Contains(c))
               ?? Initiative.AvailableColors[existing.Count % Initiative.AvailableColors.Length];
    }

    private Task LogAsync(ActivityType type, EntityType entityType, Guid id, string name, string description, CancellationToken cancellationToken) =>
        _activityService.LogActivityAsync(type, entityType, id, name, description, cancellationToken);

    internal static QuarterDto MapQuarter(Quarter quarter) => new()
    {
        Id = quarter.Id,
        Year = quarter.Year,
        QuarterNumber = quarter.QuarterNumber,
        Name = quarter.Name,
        Status = quarter.Status,
        OkrReference = quarter.OkrReference,
        ProductRoadmapPercent = quarter.ProductRoadmapPercent,
        TechRoadmapPercent = quarter.TechRoadmapPercent,
        MaintenancePercent = quarter.MaintenancePercent,
        CommittedAt = quarter.CommittedAt,
        CreatedAt = quarter.CreatedAt,
        UpdatedAt = quarter.UpdatedAt
    };

    #endregion
}
