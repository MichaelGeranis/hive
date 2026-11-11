using System.Text;
using System.Text.Json;
using Hive.Application.DTOs;
using Hive.Application.Formatting;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Reads a quarter's plan and works out what it adds up to. Nothing here is stored except the
/// commitment's baseline and what a draft applies.
/// </summary>
public class PlanningBoardService : IPlanningBoardService
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
    private readonly ILeaveRepository _leaveRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProjectProductRepository _projectProductRepository;
    private readonly IProjectKnowledgeRepository _knowledgeRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IActivityService _activityService;

    private static readonly JsonSerializerOptions BaselineJson = new(JsonSerializerDefaults.Web);

    public PlanningBoardService(
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
        ILeaveRepository leaveRepository,
        IProductRepository productRepository,
        IProjectProductRepository projectProductRepository,
        IProjectKnowledgeRepository knowledgeRepository,
        ITeamTaskRepository taskRepository,
        IParentRepository parentRepository,
        IAppSettingsService appSettingsService,
        IActivityService activityService)
    {
        _quarterRepository = quarterRepository;
        _initiativeRepository = initiativeRepository;
        _allocationRepository = allocationRepository;
        _sprintGoalRepository = sprintGoalRepository;
        _dependencyRepository = dependencyRepository;
        _memberRepository = memberRepository;
        _externalDependencyRepository = externalDependencyRepository;
        _quarterMemberRepository = quarterMemberRepository;
        _initiativeProductRepository = initiativeProductRepository;
        _sprintRepository = sprintRepository;
        _directReportRepository = directReportRepository;
        _leaveRepository = leaveRepository;
        _productRepository = productRepository;
        _projectProductRepository = projectProductRepository;
        _knowledgeRepository = knowledgeRepository;
        _taskRepository = taskRepository;
        _parentRepository = parentRepository;
        _appSettingsService = appSettingsService;
        _activityService = activityService;
    }

    #region Loading

    /// <summary>Everything stored about a quarter's plan, and the plan computed from it.</summary>
    private sealed record PlanData(
        QuarterPlan Plan,
        IReadOnlyList<DirectReport> People,
        IReadOnlyList<Product> Products,
        IReadOnlyList<InitiativeProduct> InitiativeProducts,
        IReadOnlyList<ExternalDependency> ExternalDependencies,
        IReadOnlyList<InitiativeDependency> Dependencies,
        IReadOnlyList<SprintGoal> SprintNotes,
        IReadOnlyList<TeamTask> Tasks,
        AppSettingsDto Settings);

    private async Task<PlanData> LoadAsync(Guid quarterId, CancellationToken cancellationToken)
    {
        var quarter = await _quarterRepository.GetByIdAsync(quarterId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Quarter), quarterId);
        return await LoadAsync(quarter, withKnowledge: true, cancellationToken);
    }

    private async Task<PlanData> LoadAsync(Quarter quarter, bool withKnowledge, CancellationToken cancellationToken)
    {
        var sprints = await _sprintRepository.GetByYearQuarterAsync(quarter.Year, quarter.QuarterNumber, cancellationToken);
        var people = await _directReportRepository.GetAllAsync(cancellationToken);
        var roster = QuarterPlan.ResolveRoster(await _quarterMemberRepository.GetByQuarterAsync(quarter.Id, cancellationToken), people);
        var initiatives = await _initiativeRepository.GetByQuarterAsync(quarter.Id, cancellationToken);
        var allocations = await _allocationRepository.GetByQuarterAsync(quarter.Id, cancellationToken);
        var members = await _memberRepository.GetByQuarterAsync(quarter.Id, cancellationToken);
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);
        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sizeMap = settings.TshirtSizeMappings
            .GroupBy(m => m.Size.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First().Sprints);

        var products = await _productRepository.GetAllAsync(cancellationToken);
        var productIds = products.Select(p => p.Id).ToHashSet();
        var initiativeProducts = (await _initiativeProductRepository.GetByQuarterAsync(quarter.Id, cancellationToken))
            .Where(ip => productIds.Contains(ip.ProductId))
            .ToList();

        IReadOnlyList<TeamTask> tasks = Array.Empty<TeamTask>();
        var knowledge = KnowledgeIndex.Empty;
        if (withKnowledge)
        {
            tasks = await _taskRepository.GetAllAsync(cancellationToken);
            knowledge = await BuildKnowledgeAsync(initiativeProducts, products, tasks, cancellationToken);
        }

        var plan = new QuarterPlan(quarter, sprints, roster, initiatives, allocations, members, leaves, sizeMap, knowledge);

        return new PlanData(
            plan,
            people,
            products,
            initiativeProducts,
            withKnowledge ? await _externalDependencyRepository.GetByQuarterAsync(quarter.Id, cancellationToken) : Array.Empty<ExternalDependency>(),
            withKnowledge ? await _dependencyRepository.GetByQuarterAsync(quarter.Id, cancellationToken) : Array.Empty<InitiativeDependency>(),
            withKnowledge ? await _sprintGoalRepository.GetByQuarterAsync(quarter.Id, cancellationToken) : Array.Empty<SprintGoal>(),
            tasks,
            settings);
    }

    /// <summary>
    /// For each initiative with products: each person's best knowledge level on the projects
    /// serving those products, and how many tasks (epics aside) they have been assigned whose
    /// components name one of them.
    /// </summary>
    private async Task<KnowledgeIndex> BuildKnowledgeAsync(
        IReadOnlyList<InitiativeProduct> initiativeProducts,
        IReadOnlyList<Product> products,
        IReadOnlyList<TeamTask> tasks,
        CancellationToken cancellationToken)
    {
        if (initiativeProducts.Count == 0) return KnowledgeIndex.Empty;

        var projectsByProduct = (await _projectProductRepository.GetAllAsync(cancellationToken))
            .GroupBy(pp => pp.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(pp => pp.ProjectId).ToList());
        var knowledge = await _knowledgeRepository.GetAllAsync(cancellationToken);
        var productById = products.ToDictionary(p => p.Id);

        var levels = new Dictionary<(Guid, Guid), int>();
        var taskCounts = new Dictionary<(Guid, Guid), int>();
        var withProjects = new HashSet<Guid>();

        foreach (var group in initiativeProducts.GroupBy(ip => ip.InitiativeId))
        {
            var initiativeId = group.Key;
            var projectIds = group
                .SelectMany(ip => projectsByProduct.TryGetValue(ip.ProductId, out var ids) ? ids : new List<Guid>())
                .ToHashSet();
            if (projectIds.Count > 0) withProjects.Add(initiativeId);

            foreach (var entry in knowledge.Where(k => projectIds.Contains(k.ProjectId)))
            {
                var key = (initiativeId, entry.DirectReportId);
                levels[key] = Math.Max(levels.GetValueOrDefault(key), entry.KnowledgeLevel);
            }

            var groupProducts = group.Select(ip => productById[ip.ProductId]).ToList();
            foreach (var task in tasks.Where(t => t.AssigneeId.HasValue && t.Type != TaskType.Epic))
            {
                if (groupProducts.Any(p => p.IsNamedIn(task.Components)))
                {
                    var key = (initiativeId, task.AssigneeId!.Value);
                    taskCounts[key] = taskCounts.GetValueOrDefault(key) + 1;
                }
            }
        }

        return new KnowledgeIndex(levels, taskCounts, withProjects);
    }

    #endregion

    #region Board

    public async Task<PlanningBoardDto> GetBoardAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        var plan = data.Plan;
        var parents = await _parentRepository.GetAllAsync(cancellationToken);

        var waterline = plan.Waterline();
        var insights = PlanningInsights.Build(plan, data.ExternalDependencies, data.Dependencies);
        var initiatives = OrderForDisplay(plan)
            .Select(i => MapInitiative(i, data))
            .ToList();

        var previous = plan.Quarter.Previous();
        var previousQuarter = await _quarterRepository.GetByYearQuarterAsync(previous.Year, previous.QuarterNumber, cancellationToken);
        var knownTeams = data.ExternalDependencies.Select(d => d.Team).ToList();
        if (previousQuarter is not null)
        {
            knownTeams.AddRange((await _externalDependencyRepository.GetByQuarterAsync(previousQuarter.Id, cancellationToken)).Select(d => d.Team));
        }

        return new PlanningBoardDto
        {
            Quarter = QuarterlyPlanningService.MapQuarter(plan.Quarter),
            Sprints = plan.Sprints.Select(s => new PlanningSprintDto
            {
                Id = s.Id,
                Name = s.Name,
                SprintNumber = s.SprintNumber,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                WorkingDays = SprintAvailability.WorkingDays(s),
                Notes = data.SprintNotes.FirstOrDefault(n => n.SprintId == s.Id)?.Notes ?? string.Empty
            }).ToList(),
            Roster = plan.Roster.Select(r => MapRosterMember(r, plan)).ToList(),
            Initiatives = initiatives,
            Dependencies = data.Dependencies.Select(d => new InitiativeDependencyDto
            {
                Id = d.Id,
                DependentInitiativeId = d.DependentInitiativeId,
                DependentInitiativeName = plan.Initiatives.FirstOrDefault(i => i.Id == d.DependentInitiativeId)?.Name ?? string.Empty,
                DependencyInitiativeId = d.DependencyInitiativeId,
                DependencyInitiativeName = plan.Initiatives.FirstOrDefault(i => i.Id == d.DependencyInitiativeId)?.Name ?? string.Empty,
                Type = d.Type,
                Notes = d.Notes,
                CreatedAt = d.CreatedAt
            }).ToList(),
            Allocations = plan.Allocations.Select(a => new AllocationDto
            {
                Id = a.Id,
                InitiativeId = a.InitiativeId,
                DirectReportId = a.DirectReportId,
                SprintId = a.SprintId,
                Percent = a.Percent,
                Effort = QuarterPlan.Round(plan.Effort(a))
            }).ToList(),
            Capacity = BuildCapacity(plan),
            Waterline = waterline,
            Mix = await BuildMixAsync(plan, previousQuarter, data, parents, cancellationToken),
            Insights = insights,
            Checklist = BuildChecklist(data, waterline, insights),
            SizeMappings = data.Settings.TshirtSizeMappings.OrderBy(m => m.Sprints).ToList(),
            Products = data.Products.OrderBy(p => p.Name).Select(p => new ProductRefDto { Id = p.Id, Name = p.Name }).ToList(),
            People = data.People.OrderBy(p => p.FullName).Select(p => new PersonRefDto { Id = p.Id, Name = p.FullName, IsDirect = p.IsDirect }).ToList(),
            KnownTeams = knownTeams.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList()
        };
    }

    private static IEnumerable<Initiative> OrderForDisplay(QuarterPlan plan) =>
        plan.PriorityOrder()
            .Concat(plan.Initiatives.Where(i => i.Status == InitiativeStatus.Dropped && !i.IsBucket).OrderBy(i => i.Rank))
            .Concat(plan.Initiatives.Where(i => i.IsBucket).OrderBy(i => i.WorkType));

    private static RosterMemberDto MapRosterMember(RosterEntry entry, QuarterPlan plan) => new()
    {
        DirectReportId = entry.Person.Id,
        Name = entry.Person.FullName,
        IsDirect = entry.Person.IsDirect,
        CapacityPercent = entry.CapacityPercent,
        TotalSupply = QuarterPlan.Round(plan.PersonSupply(entry.Person.Id)),
        TotalDays = QuarterPlan.Round(plan.PersonDays(entry.Person.Id)),
        Sprints = plan.Sprints.Select(s => new MemberSprintDto
        {
            SprintId = s.Id,
            AvailablePercent = (int)Math.Round(plan.Availability(entry.Person.Id, s.Id) * 100),
            LeaveDays = plan.LeaveDays(entry.Person.Id, s.Id),
            Supply = QuarterPlan.Round(plan.Supply(entry.Person.Id, s.Id)),
            AllocatedPercent = plan.AllocatedPercent(entry.Person.Id, s.Id)
        }).ToList()
    };

    private InitiativeDto MapInitiative(Initiative initiative, PlanData data)
    {
        var plan = data.Plan;
        var names = data.People.ToDictionary(p => p.Id, p => p.FullName);
        var members = plan.Members.Where(m => m.InitiativeId == initiative.Id).ToList();
        var memberIds = members.Select(m => m.DirectReportId).ToHashSet();

        var suggestions = initiative.IsBucket
            ? new List<PersonSuggestionDto>()
            : plan.Roster
                .Select(r => new PersonSuggestionDto
                {
                    DirectReportId = r.Person.Id,
                    Name = r.Person.FullName,
                    KnowledgeLevel = plan.Knowledge.Level(initiative.Id, r.Person.Id),
                    TaskCount = plan.Knowledge.TaskCount(initiative.Id, r.Person.Id),
                    FreeEffort = plan.FreeEffort(r.Person.Id),
                    IsMember = memberIds.Contains(r.Person.Id)
                })
                .Where(s => s.KnowledgeLevel > 0 || s.TaskCount > 0)
                .OrderByDescending(s => s.KnowledgeLevel)
                .ThenByDescending(s => s.TaskCount)
                .ThenByDescending(s => s.FreeEffort)
                .Take(6)
                .ToList();

        return new InitiativeDto
        {
            Id = initiative.Id,
            QuarterId = initiative.QuarterId,
            Name = initiative.Name,
            Description = initiative.Description,
            Color = initiative.Color,
            TshirtSize = initiative.TshirtSize,
            Url = initiative.Url,
            WorkType = (int)initiative.WorkType,
            Kind = (int)initiative.Kind,
            Status = (int)initiative.Status,
            Rank = initiative.Rank,
            Readiness = (int)initiative.Readiness,
            DecisionNote = initiative.DecisionNote,
            EarliestSprintId = initiative.EarliestSprintId,
            ArrivedOn = initiative.ArrivedOn,
            DueDate = initiative.DueDate,
            Effort = plan.Estimate(initiative),
            AllocatedEffort = plan.AllocatedEffort(initiative.Id),
            Forecast = initiative.IsBucket ? null : plan.Forecast(initiative),
            Members = members
                .OrderByDescending(m => m.IsLead)
                .ThenBy(m => names.GetValueOrDefault(m.DirectReportId))
                .Select(m => new InitiativeMemberDto
                {
                    Id = m.Id,
                    InitiativeId = m.InitiativeId,
                    DirectReportId = m.DirectReportId,
                    DirectReportName = names.GetValueOrDefault(m.DirectReportId) ?? string.Empty,
                    Role = (int)m.Role,
                    KnowledgeLevel = plan.Knowledge.Level(initiative.Id, m.DirectReportId),
                    CreatedAt = m.CreatedAt
                }).ToList(),
            Products = data.InitiativeProducts
                .Where(ip => ip.InitiativeId == initiative.Id)
                .Select(ip => data.Products.First(p => p.Id == ip.ProductId))
                .Select(p => new ProductRefDto { Id = p.Id, Name = p.Name })
                .ToList(),
            ExternalDependencies = data.ExternalDependencies
                .Where(d => d.InitiativeId == initiative.Id)
                .Select(d => new ExternalDependencyDto { Id = d.Id, InitiativeId = d.InitiativeId, Team = d.Team, Need = d.Need, Status = (int)d.Status })
                .ToList(),
            Suggestions = suggestions,
            CreatedAt = initiative.CreatedAt,
            UpdatedAt = initiative.UpdatedAt
        };
    }

    private static CapacityDto BuildCapacity(QuarterPlan plan) => new()
    {
        TotalSupply = QuarterPlan.Round(plan.TotalSupply),
        TotalDays = QuarterPlan.Round(plan.TotalDays),
        TotalAllocated = plan.TotalAllocated,
        Reservations = new[] { WorkType.Maintenance, WorkType.TechRoadmap }
            .Select(t => new ReservationDto { WorkType = (int)t, Percent = plan.Quarter.GetMixPercent(t), Effort = plan.Reservation(t) })
            .ToList(),
        OptionCapacity = plan.OptionCapacity,
        Sprints = plan.Sprints.Select(s => new PlanSprintCapacityDto
        {
            SprintId = s.Id,
            Supply = QuarterPlan.Round(plan.SprintSupply(s.Id)),
            FullSupply = QuarterPlan.Round(plan.SprintFullSupply()),
            Allocated = QuarterPlan.Round(plan.SprintAllocated(s.Id)),
            Mix = plan.PlannedMix(s.Id)
        }).ToList()
    };

    /// <summary>
    /// The target mix against the plan, next to what last quarter looked like: its plan, and
    /// the share of its tickets that were unplanned work (bugs, support, maintenance), and the
    /// team's work so far as a mix.
    /// </summary>
    private async Task<MixDto> BuildMixAsync(QuarterPlan plan, Quarter? previousQuarter, PlanData data, IReadOnlyList<Parent> parents, CancellationToken cancellationToken)
    {
        MixSharesDto? previousMix = null;
        int? unplanned = null;
        var recentCount = 0;

        if (previousQuarter is not null)
        {
            var previousInitiatives = (await _initiativeRepository.GetByQuarterAsync(previousQuarter.Id, cancellationToken)).ToDictionary(i => i.Id);
            var previousAllocations = await _allocationRepository.GetByQuarterAsync(previousQuarter.Id, cancellationToken);
            if (previousAllocations.Count > 0)
            {
                previousMix = QuarterPlan.Shares(previousAllocations
                    .Where(a => previousInitiatives.ContainsKey(a.InitiativeId))
                    .GroupBy(a => previousInitiatives[a.InitiativeId].WorkType)
                    .ToDictionary(g => g.Key, g => (decimal)g.Sum(a => a.Percent)));
            }
        }

        // The same work categories as the Dashboard's Work mix, over every task
        var epicNames = data.Settings.WorkCategoryEpicNames.ToDictionary(
            entry => Enum.Parse<WorkCategory>(entry.Key),
            entry => (IReadOnlyList<string>)entry.Value);
        var workCategories = new WorkCategories(parents, data.Tasks, epicNames);
        var pastWork = WorkMix.ByCategory(WorkMix.ExcludeParentTasks(data.Tasks, parents), workCategories);

        var (year, number) = plan.Quarter.Previous();
        var previousSprints = (await _sprintRepository.GetByYearQuarterAsync(year, number, cancellationToken))
            .Select(s => s.Name)
            .ToList();
        if (previousSprints.Count > 0)
        {
            var labels = data.Settings.MaintenanceLabels;
            var recent = data.Tasks
                .Where(t => t.Type != TaskType.Epic && previousSprints.Any(t.IsInSprint))
                .ToList();
            recentCount = recent.Count;
            if (recent.Count > 0)
            {
                var count = recent.Count(t => t.Type is TaskType.Bug or TaskType.Support || labels.Any(t.HasLabel));
                unplanned = (int)Math.Round(count * 100m / recent.Count);
            }
        }

        return new MixDto
        {
            Target = plan.TargetMix(),
            Planned = plan.PlannedMix(),
            HasPlan = plan.Allocations.Count > 0,
            PreviousQuarter = previousMix,
            RecentUnplannedPercent = unplanned,
            RecentTaskCount = recentCount,
            PastWork = WorkMix.AsPlanningMix(pastWork),
            PastWorkStoryPoints = pastWork.Sum(c => c.StoryPoints)
        };
    }


    private static PlanningChecklistDto BuildChecklist(PlanData data, WaterlineDto waterline, IReadOnlyList<PlanningInsightDto> insights)
    {
        var plan = data.Plan;
        var options = plan.Initiatives.Where(i => !i.IsBucket && i.IsInPlay).ToList();
        var aboveLine = waterline.Rows.Where(r => r.AboveLine).Select(r => r.InitiativeId).ToHashSet();
        var takenOn = options.Where(i => i.Status == InitiativeStatus.Committed || aboveLine.Contains(i.Id)).ToList();

        return new PlanningChecklistDto
        {
            OptionCount = options.Count,
            InDiscoveryCount = options.Count(i => i.Status == InitiativeStatus.InDiscovery),
            // Sizing comes with readiness, so only options ready for commitment or decided count
            UnsizedCount = options.Count(i => Initiative.CanBeSized(i.Status) && plan.Estimate(i) is null),
            NotReadyCount = options.Count(i => i.HasReadinessIssues && i.Status != InitiativeStatus.Delivered),
            OpenDependencyCount = data.ExternalDependencies.Count(d => d.IsOpen && options.Any(o => o.Id == d.InitiativeId)),
            WithoutLeadCount = takenOn.Count(i => !plan.Members.Any(m => m.InitiativeId == i.Id && m.IsLead)),
            UndecidedCount = options.Count(i => i.Status is InitiativeStatus.InDiscovery or InitiativeStatus.InSolutionDesign or InitiativeStatus.ReadyForCommitment),
            UnallocatedCommittedCount = options.Count(i => i.Status == InitiativeStatus.Committed && plan.AllocatedEffort(i.Id) <= 0),
            HasSprints = plan.Sprints.Count > 0,
            HasRoster = plan.Roster.Count > 0,
            MixOnTarget = plan.Allocations.Count > 0 && insights.All(i => i.Type != "MixOffTarget"),
            Committed = plan.Quarter.IsCommitted
        };
    }

    #endregion

    #region Draft and scenarios

    public async Task<PlanDraftDto> DraftAsync(Guid quarterId, bool apply, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        var plan = data.Plan;
        var drafted = plan.Draft();
        var after = plan.WithAllocations(drafted);

        if (apply)
        {
            var existing = plan.Allocations.ToList();
            foreach (var allocation in drafted)
            {
                var match = existing.FirstOrDefault(a =>
                    a.InitiativeId == allocation.InitiativeId &&
                    a.DirectReportId == allocation.DirectReportId &&
                    a.SprintId == allocation.SprintId);
                if (match is null)
                {
                    await _allocationRepository.AddAsync(allocation, cancellationToken);
                    existing.Add(allocation);
                }
                else
                {
                    match.SetPercent(Math.Min(100, match.Percent + allocation.Percent));
                    await _allocationRepository.UpdateAsync(match, cancellationToken);
                }
            }
        }

        var names = data.People.ToDictionary(p => p.Id, p => p.FullName);
        return new PlanDraftDto
        {
            Applied = apply,
            Allocations = drafted.Select(a => new DraftAllocationDto
            {
                InitiativeId = a.InitiativeId,
                InitiativeName = plan.Initiatives.First(i => i.Id == a.InitiativeId).Name,
                DirectReportId = a.DirectReportId,
                DirectReportName = names.GetValueOrDefault(a.DirectReportId) ?? string.Empty,
                SprintId = a.SprintId,
                SprintName = plan.Sprints.First(s => s.Id == a.SprintId).Name,
                Percent = a.Percent
            }).ToList(),
            Options = CompareOptions(plan, after)
        };
    }

    public async Task<ScenarioDto> RunScenarioAsync(Guid quarterId, ScenarioRequestDto request, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        var plan = data.Plan;

        var sprintIds = plan.Sprints.Select(s => s.Id).ToHashSet();
        var absences = request.Absences
            .SelectMany(a => a.SprintIds.Where(sprintIds.Contains).Select(s => (a.DirectReportId, s)))
            .ToList();
        var delays = request.Delays.Select(d => (d.InitiativeId, d.NotBeforeSprintId)).ToList();
        var after = plan.WithScenario(absences, delays);

        return new ScenarioDto
        {
            SupplyBefore = QuarterPlan.Round(plan.TotalSupply),
            SupplyAfter = QuarterPlan.Round(after.TotalSupply),
            OptionCapacityBefore = plan.OptionCapacity,
            OptionCapacityAfter = after.OptionCapacity,
            Options = CompareOptions(plan, after)
        };
    }

    private static IReadOnlyList<ScenarioOptionDto> CompareOptions(QuarterPlan before, QuarterPlan after)
    {
        var lineBefore = before.Waterline().Rows.ToDictionary(r => r.InitiativeId, r => r.AboveLine);
        var lineAfter = after.Waterline().Rows.ToDictionary(r => r.InitiativeId, r => r.AboveLine);
        return before.PriorityOrder()
            .Select(i => new ScenarioOptionDto
            {
                InitiativeId = i.Id,
                Name = i.Name,
                Before = before.Forecast(i),
                After = after.Forecast(i),
                AboveLineBefore = lineBefore.GetValueOrDefault(i.Id),
                AboveLineAfter = lineAfter.GetValueOrDefault(i.Id)
            })
            .ToList();
    }

    #endregion

    #region Commitment and review

    /// <summary>
    /// Commits the plan as it stands: the options, their decisions and forecasts become the
    /// baseline the review compares with.
    /// </summary>
    public async Task<QuarterDto> CommitAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        var plan = data.Plan;
        var waterline = plan.Waterline().Rows.ToDictionary(r => r.InitiativeId, r => r.AboveLine);

        var baseline = new BaselineDto
        {
            CommittedAt = DateTime.UtcNow,
            TotalSupply = QuarterPlan.Round(plan.TotalSupply),
            OptionCapacity = plan.OptionCapacity,
            PlannedMix = plan.PlannedMix(),
            Options = plan.Initiatives.Where(i => !i.IsBucket).Select(i =>
            {
                var forecast = plan.Forecast(i);
                return new BaselineOptionDto
                {
                    InitiativeId = i.Id,
                    Name = i.Name,
                    Status = (int)i.Status,
                    TshirtSize = i.TshirtSize,
                    Estimate = forecast.Estimate,
                    Allocated = forecast.Allocated,
                    CoveredPercent = forecast.CoveredPercent,
                    DoneSprintName = forecast.DoneSprintName,
                    AboveLine = waterline.GetValueOrDefault(i.Id)
                };
            }).ToList()
        };

        plan.Quarter.Commit(JsonSerializer.Serialize(baseline, BaselineJson));
        await _quarterRepository.UpdateAsync(plan.Quarter, cancellationToken);

        var committed = plan.Initiatives.Count(i => !i.IsBucket && i.Status == InitiativeStatus.Committed);
        await _activityService.LogActivityAsync(ActivityType.StatusChanged, EntityType.Quarter, plan.Quarter.Id, plan.Quarter.Name,
            $"The {plan.Quarter.Name} plan was committed with {committed} option{(committed == 1 ? "" : "s")}", cancellationToken);

        return QuarterlyPlanningService.MapQuarter(plan.Quarter);
    }

    public async Task<PlanReviewDto> GetReviewAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        var plan = data.Plan;
        var baseline = ReadBaseline(plan.Quarter);

        var options = plan.Initiatives
            .Where(i => !i.IsBucket)
            .OrderBy(i => i.Status == InitiativeStatus.Dropped ? 1 : 0)
            .ThenBy(i => i.Kind == InitiativeKind.Leftover ? 0 : 1)
            .ThenBy(i => i.Rank == 0 ? int.MaxValue : i.Rank)
            .Select(i =>
            {
                var forecast = plan.Forecast(i);
                return new ReviewOptionDto
                {
                    InitiativeId = i.Id,
                    Name = i.Name,
                    Color = i.Color,
                    AtCommit = baseline?.Options.FirstOrDefault(b => b.InitiativeId == i.Id),
                    StatusNow = (int)i.Status,
                    AllocatedNow = forecast.Allocated,
                    CoveredNow = forecast.CoveredPercent,
                    DoneSprintNow = forecast.DoneSprintName
                };
            })
            .ToList();

        return new PlanReviewDto
        {
            CommittedAt = plan.Quarter.CommittedAt,
            Options = options,
            Summary = BuildSummary(data)
        };
    }

    private static BaselineDto? ReadBaseline(Quarter quarter)
    {
        if (string.IsNullOrWhiteSpace(quarter.Baseline)) return null;
        try
        {
            return JsonSerializer.Deserialize<BaselineDto>(quarter.Baseline, BaselineJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The commitment in plain text, to paste to the PM: what is committed and when it lands,
    /// what is a stretch, what was dropped and why, and what the plan rests on.
    /// </summary>
    private static string BuildSummary(PlanData data)
    {
        var plan = data.Plan;
        var names = data.People.ToDictionary(p => p.Id, p => p.FullName);
        var text = new StringBuilder();
        text.AppendLine($"{plan.Quarter.Name} plan" + (plan.Quarter.CommittedAt.HasValue ? $", committed {plan.Quarter.CommittedAt.Value.ToLocalTime().ToDisplayDate()}" : ", not committed yet"));
        text.AppendLine($"Capacity: {plan.TotalSupply:0.#} dev-sprints; {plan.OptionCapacity:0.#} for options after maintenance ({plan.Quarter.MaintenancePercent}%) and tech roadmap ({plan.Quarter.TechRoadmapPercent}%).");

        void Section(string title, InitiativeStatus status, bool withForecast)
        {
            var items = plan.PriorityOrder().Concat(plan.Initiatives.Where(i => i.Status == InitiativeStatus.Dropped && !i.IsBucket))
                .Where(i => i.Status == status)
                .ToList();
            if (items.Count == 0) return;

            text.AppendLine();
            text.AppendLine($"{title}:");
            foreach (var i in items)
            {
                var line = new StringBuilder(i.TshirtSize is null ? $"- {i.Name}" : $"- {i.Name} ({i.TshirtSize})");
                var lead = plan.Members.FirstOrDefault(m => m.InitiativeId == i.Id && m.IsLead);
                if (lead is not null) line.Append($", led by {names.GetValueOrDefault(lead.DirectReportId)}");
                if (withForecast)
                {
                    var forecast = plan.Forecast(i);
                    line.Append(forecast.DeliveredInQuarter
                        ? $", done by {forecast.DoneSprintName}"
                        : forecast.CoveredPercent.HasValue ? $", {forecast.CoveredPercent}% covered this quarter" : string.Empty);
                }

                if (i.HasReadinessIssues) line.Append($"; pending: {PlanningInsights.DescribeReadiness(i.Readiness)}");
                var open = data.ExternalDependencies.Where(d => d.InitiativeId == i.Id && d.IsOpen).Select(d => d.Team).Distinct().ToList();
                if (open.Count > 0) line.Append($"; needs {string.Join(", ", open)}");
                if (!string.IsNullOrWhiteSpace(i.DecisionNote)) line.Append($" — {i.DecisionNote}");
                text.AppendLine(line.ToString());
            }
        }

        Section("Committed", InitiativeStatus.Committed, true);
        Section("Delivered", InitiativeStatus.Delivered, false);
        Section("Stretch", InitiativeStatus.Stretch, true);
        Section("Ready for commitment", InitiativeStatus.ReadyForCommitment, false);
        Section("In solution design", InitiativeStatus.InSolutionDesign, false);
        Section("In discovery", InitiativeStatus.InDiscovery, false);
        Section("Dropped", InitiativeStatus.Dropped, false);
        return text.ToString().TrimEnd();
    }

    /// <summary>
    /// Every delivered option across quarters, by size: the dev-sprints its size stands for,
    /// against the dev-sprints its quarter's plan actually gave it.
    /// </summary>
    public async Task<CalibrationDto> GetCalibrationAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _appSettingsService.GetAsync(cancellationToken);
        var sizeMap = settings.TshirtSizeMappings
            .GroupBy(m => m.Size.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First().Sprints);

        var actuals = new List<(string Size, decimal Actual)>();
        foreach (var quarter in await _quarterRepository.GetAllAsync(cancellationToken))
        {
            var initiatives = await _initiativeRepository.GetByQuarterAsync(quarter.Id, cancellationToken);
            if (!initiatives.Any(i => i.Status == InitiativeStatus.Delivered)) continue;

            var plan = (await LoadAsync(quarter, withKnowledge: false, cancellationToken)).Plan;
            actuals.AddRange(plan.Initiatives
                .Where(i => i.Status == InitiativeStatus.Delivered)
                .Where(i => i.TshirtSize is not null)
                .Select(i => (i.TshirtSize!, plan.AllocatedEffort(i.Id))));
        }

        var sizes = actuals
            .GroupBy(a => a.Size)
            .Select(g =>
            {
                decimal? estimate = sizeMap.TryGetValue(g.Key, out var e) ? e : null;
                var average = QuarterPlan.Round(g.Average(a => a.Actual));
                return new CalibrationRowDto
                {
                    Size = g.Key,
                    Estimate = estimate,
                    DeliveredCount = g.Count(),
                    AverageActual = average,
                    Ratio = estimate is > 0 ? QuarterPlan.Round(average / estimate.Value) : null
                };
            })
            .OrderBy(r => r.Estimate ?? decimal.MaxValue)
            .ToList();

        return new CalibrationDto { Sizes = sizes, DeliveredCount = actuals.Count };
    }

    #endregion

    #region Export

    public async Task<byte[]> ExportToExcelAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var data = await LoadAsync(quarterId, cancellationToken);
        return PlanCatalogueExport.Build(data.Plan, data.People, data.ExternalDependencies, data.SprintNotes);
    }

    #endregion
}
