using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Services;

/// <summary>
/// One person on the plan and the share of their time the team can plan with.
/// </summary>
internal sealed record RosterEntry(DirectReport Person, int CapacityPercent)
{
    public decimal Factor => CapacityPercent / 100m;
}

/// <summary>
/// Who knows what: for each initiative, each person's best knowledge level on the projects
/// behind its products, and how many tasks they have done on those products.
/// </summary>
internal sealed class KnowledgeIndex
{
    public static readonly KnowledgeIndex Empty = new(new Dictionary<(Guid, Guid), int>(), new Dictionary<(Guid, Guid), int>(), new HashSet<Guid>());

    private readonly IReadOnlyDictionary<(Guid InitiativeId, Guid PersonId), int> _levels;
    private readonly IReadOnlyDictionary<(Guid InitiativeId, Guid PersonId), int> _taskCounts;
    private readonly IReadOnlySet<Guid> _initiativesWithProjects;

    public KnowledgeIndex(
        IReadOnlyDictionary<(Guid, Guid), int> levels,
        IReadOnlyDictionary<(Guid, Guid), int> taskCounts,
        IReadOnlySet<Guid> initiativesWithProjects)
    {
        _levels = levels;
        _taskCounts = taskCounts;
        _initiativesWithProjects = initiativesWithProjects;
    }

    public int Level(Guid initiativeId, Guid personId) =>
        _levels.TryGetValue((initiativeId, personId), out var level) ? level : 0;

    public int TaskCount(Guid initiativeId, Guid personId) =>
        _taskCounts.TryGetValue((initiativeId, personId), out var count) ? count : 0;

    /// <summary>Whether the initiative's products lead to any project, so knowledge can be read at all.</summary>
    public bool HasProjects(Guid initiativeId) => _initiativesWithProjects.Contains(initiativeId);
}

/// <summary>
/// A quarter's plan as numbers. Everything the planning page shows about capacity is derived
/// here from the stored facts (roster, leave, sizes, allocations) and never stored.
///
/// The unit is the dev-sprint: one person for one whole sprint. A person brings their
/// availability in a sprint (working days less leave) times their planned capacity; an
/// allocation is worth its share of that.
/// </summary>
internal sealed class QuarterPlan
{
    public const int DraftStep = 25;

    private readonly Dictionary<(Guid PersonId, Guid SprintId), decimal> _availability = new();
    private readonly Dictionary<(Guid PersonId, Guid SprintId), int> _leaveDays = new();
    private readonly Dictionary<Guid, RosterEntry> _roster;
    private readonly Dictionary<Guid, Initiative> _initiatives;
    private readonly HashSet<(Guid PersonId, Guid SprintId)> _absences;

    public Quarter Quarter { get; }
    public IReadOnlyList<Sprint> Sprints { get; }
    public IReadOnlyList<RosterEntry> Roster { get; }
    public IReadOnlyList<Initiative> Initiatives { get; }
    public IReadOnlyList<Allocation> Allocations { get; }
    public IReadOnlyList<InitiativeMember> Members { get; }
    public IReadOnlyDictionary<string, decimal> SizeMap { get; }
    public KnowledgeIndex Knowledge { get; }

    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<Leave>> _leaves;

    public QuarterPlan(
        Quarter quarter,
        IEnumerable<Sprint> sprints,
        IEnumerable<RosterEntry> roster,
        IEnumerable<Initiative> initiatives,
        IEnumerable<Allocation> allocations,
        IEnumerable<InitiativeMember> members,
        IEnumerable<Leave> leaves,
        IReadOnlyDictionary<string, decimal> sizeMap,
        KnowledgeIndex? knowledge = null,
        IEnumerable<(Guid PersonId, Guid SprintId)>? absences = null)
    {
        Quarter = quarter;
        Sprints = sprints.OrderBy(s => s.GetSortOrder()).ThenBy(s => s.StartDate).ToList();
        Roster = roster.ToList();
        Initiatives = initiatives.ToList();
        Allocations = allocations.ToList();
        Members = members.ToList();
        SizeMap = sizeMap.ToDictionary(kv => kv.Key.Trim().ToUpperInvariant(), kv => kv.Value);
        Knowledge = knowledge ?? KnowledgeIndex.Empty;
        _roster = Roster.ToDictionary(r => r.Person.Id);
        _initiatives = Initiatives.ToDictionary(i => i.Id);
        _absences = absences?.ToHashSet() ?? new HashSet<(Guid, Guid)>();
        _leaves = leaves
            .GroupBy(l => l.DirectReportId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Leave>)g.ToList());
    }

    /// <summary>
    /// The same plan with people taken out of some sprints, and initiatives held back until a
    /// later sprint: their allocations before it do not count.
    /// </summary>
    public QuarterPlan WithScenario(
        IEnumerable<(Guid PersonId, Guid SprintId)> absences,
        IEnumerable<(Guid InitiativeId, Guid NotBeforeSprintId)> delays)
    {
        var order = SprintOrder();
        var held = delays
            .Where(d => order.ContainsKey(d.NotBeforeSprintId))
            .ToDictionary(d => d.InitiativeId, d => order[d.NotBeforeSprintId]);

        var allocations = Allocations.Where(a =>
            !held.TryGetValue(a.InitiativeId, out var notBefore) ||
            !order.TryGetValue(a.SprintId, out var index) ||
            index >= notBefore);

        return new QuarterPlan(
            Quarter, Sprints, Roster, Initiatives, allocations, Members,
            _leaves.Values.SelectMany(l => l), SizeMap, Knowledge,
            _absences.Concat(absences));
    }

    /// <summary>The same plan with more allocations, as a draft would add them.</summary>
    public QuarterPlan WithAllocations(IEnumerable<Allocation> added) =>
        new(Quarter, Sprints, Roster, Initiatives, Allocations.Concat(added), Members,
            _leaves.Values.SelectMany(l => l), SizeMap, Knowledge, _absences);

    /// <summary>
    /// Who is on a quarter's plan: the people the manager put on it, or, until they change it,
    /// every direct report at full capacity.
    /// </summary>
    public static IReadOnlyList<RosterEntry> ResolveRoster(IReadOnlyList<QuarterMember> rows, IReadOnlyList<DirectReport> people)
    {
        if (rows.Count == 0)
        {
            return people
                .Where(p => p.IsDirect)
                .OrderBy(p => p.FullName)
                .Select(p => new RosterEntry(p, QuarterMember.DefaultCapacityPercent))
                .ToList();
        }

        var byId = people.ToDictionary(p => p.Id);
        return rows
            .Where(r => byId.ContainsKey(r.DirectReportId))
            .Select(r => new RosterEntry(byId[r.DirectReportId], r.CapacityPercent))
            .OrderBy(r => r.Person.FullName)
            .ToList();
    }

    public Dictionary<Guid, int> SprintOrder() =>
        Sprints.Select((s, i) => (s.Id, i)).ToDictionary(x => x.Id, x => x.i);

    // -----------------------------------------------------------------------------------------
    // Supply
    // -----------------------------------------------------------------------------------------

    public decimal Availability(Guid personId, Guid sprintId)
    {
        if (_absences.Contains((personId, sprintId))) return 0m;

        if (!_availability.TryGetValue((personId, sprintId), out var value))
        {
            var sprint = Sprints.FirstOrDefault(s => s.Id == sprintId);
            value = sprint is null ? 1m : SprintAvailability.Availability(sprint, LeavesOf(personId));
            _availability[(personId, sprintId)] = value;
        }

        return value;
    }

    public int LeaveDays(Guid personId, Guid sprintId)
    {
        if (!_leaveDays.TryGetValue((personId, sprintId), out var value))
        {
            var sprint = Sprints.FirstOrDefault(s => s.Id == sprintId);
            value = sprint is null ? 0 : SprintAvailability.LeaveDays(sprint, LeavesOf(personId));
            _leaveDays[(personId, sprintId)] = value;
        }

        return value;
    }

    public decimal Factor(Guid personId) => _roster.TryGetValue(personId, out var entry) ? entry.Factor : 1m;

    public bool IsOnRoster(Guid personId) => _roster.ContainsKey(personId);

    /// <summary>What a person brings to a sprint, in dev-sprints.</summary>
    public decimal Supply(Guid personId, Guid sprintId) => Availability(personId, sprintId) * Factor(personId);

    public decimal SprintSupply(Guid sprintId) => Roster.Sum(r => Supply(r.Person.Id, sprintId));

    public decimal SprintFullSupply() => Roster.Sum(r => r.Factor);

    public decimal PersonSupply(Guid personId) => Sprints.Sum(s => Supply(personId, s.Id));

    public decimal TotalSupply => Sprints.Sum(s => SprintSupply(s.Id));

    /// <summary>What a person brings to the quarter in dev-days: their supply times each sprint's working days.</summary>
    public decimal PersonDays(Guid personId) => Sprints.Sum(s => Supply(personId, s.Id) * SprintAvailability.WorkingDays(s));

    public decimal TotalDays => Roster.Sum(r => PersonDays(r.Person.Id));

    /// <summary>What the standing lanes hold back of the supply: their share of the mix.</summary>
    public decimal Reservation(WorkType workType) =>
        workType == WorkType.ProductRoadmap ? 0m : Round(TotalSupply * Quarter.GetMixPercent(workType) / 100m);

    /// <summary>
    /// What is left for options once Maintenance and the Tech roadmap have their share.
    /// </summary>
    public decimal OptionCapacity =>
        Round(TotalSupply - Reservation(WorkType.Maintenance) - Reservation(WorkType.TechRoadmap));

    // -----------------------------------------------------------------------------------------
    // Demand
    // -----------------------------------------------------------------------------------------

    /// <summary>An initiative's size in dev-sprints; null for lanes and for sizes with no mapping.</summary>
    public decimal? Estimate(Initiative initiative) =>
        initiative.IsBucket || initiative.TshirtSize is null ? null :
        SizeMap.TryGetValue(initiative.TshirtSize.ToUpperInvariant(), out var effort) ? effort : null;

    public decimal Effort(Allocation allocation) => allocation.Percent / 100m * Supply(allocation.DirectReportId, allocation.SprintId);

    public decimal AllocatedEffort(Guid initiativeId) =>
        Round(Allocations.Where(a => a.InitiativeId == initiativeId).Sum(Effort));

    public int AllocatedPercent(Guid personId, Guid sprintId) =>
        Allocations.Where(a => a.DirectReportId == personId && a.SprintId == sprintId).Sum(a => a.Percent);

    public decimal SprintAllocated(Guid sprintId) => Allocations.Where(a => a.SprintId == sprintId).Sum(Effort);

    public decimal TotalAllocated => Round(Allocations.Sum(Effort));

    /// <summary>The dev-sprints a person has in the quarter that no allocation uses yet.</summary>
    public decimal FreeEffort(Guid personId) =>
        Round(Sprints.Sum(s => Supply(personId, s.Id) * Math.Max(0, 100 - AllocatedPercent(personId, s.Id)) / 100m));

    public ForecastDto Forecast(Initiative initiative)
    {
        var estimate = Estimate(initiative);
        var cumulative = 0m;
        Guid? first = null;
        Sprint? done = null;

        foreach (var sprint in Sprints)
        {
            var effort = Allocations
                .Where(a => a.InitiativeId == initiative.Id && a.SprintId == sprint.Id)
                .Sum(Effort);
            if (effort > 0 && first is null) first = sprint.Id;
            cumulative += effort;
            if (done is null && estimate is > 0 && cumulative >= estimate.Value - 0.005m) done = sprint;
        }

        cumulative = Round(cumulative);
        return new ForecastDto
        {
            Estimate = estimate,
            Allocated = cumulative,
            CoveredPercent = estimate is > 0 ? (int)Math.Round(cumulative / estimate.Value * 100m) : null,
            FirstSprintId = first,
            DoneSprintId = done?.Id,
            DoneSprintName = done?.Name,
            DeliveredInQuarter = done is not null
        };
    }

    // -----------------------------------------------------------------------------------------
    // Waterline and mix
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// The options in priority order: leftovers first, then options by the PM's rank, unranked
    /// last. Dropped options and lanes are not on it.
    /// </summary>
    public IReadOnlyList<Initiative> PriorityOrder() =>
        Initiatives
            .Where(i => !i.IsBucket && i.Status != InitiativeStatus.Dropped)
            .OrderBy(i => i.Kind == InitiativeKind.Leftover ? 0 : 1)
            .ThenBy(i => i.Rank == 0 ? int.MaxValue : i.Rank)
            .ThenBy(i => i.CreatedAt)
            .ToList();

    public WaterlineDto Waterline()
    {
        var capacity = OptionCapacity;
        var cumulative = 0m;
        var rows = new List<WaterlineRowDto>();
        foreach (var initiative in PriorityOrder())
        {
            var effort = Estimate(initiative);
            cumulative += effort ?? 0m;
            rows.Add(new WaterlineRowDto
            {
                InitiativeId = initiative.Id,
                Name = initiative.Name,
                Color = initiative.Color,
                Kind = (int)initiative.Kind,
                Status = (int)initiative.Status,
                Rank = initiative.Rank,
                TshirtSize = initiative.TshirtSize,
                Effort = effort,
                Cumulative = Round(cumulative),
                // Only a sized option can fit: one still in discovery or design has no size yet
                AboveLine = effort.HasValue && cumulative <= capacity + 0.005m
            });
        }

        return new WaterlineDto
        {
            Capacity = capacity,
            Rows = rows,
            CommittedEffort = Round(PriorityOrder()
                .Where(i => i.Status is InitiativeStatus.Committed or InitiativeStatus.Delivered)
                .Sum(i => Estimate(i) ?? 0m))
        };
    }

    public bool IsAboveLine(Guid initiativeId) =>
        Waterline().Rows.FirstOrDefault(r => r.InitiativeId == initiativeId)?.AboveLine ?? false;

    public MixSharesDto PlannedMix(Guid? sprintId = null)
    {
        var efforts = Allocations
            .Where(a => sprintId is null || a.SprintId == sprintId)
            .Select(a => (Type: _initiatives.TryGetValue(a.InitiativeId, out var i) ? i.WorkType : WorkType.ProductRoadmap, Effort: Effort(a)))
            .ToList();
        return Shares(efforts.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Sum(e => e.Effort)));
    }

    public MixSharesDto TargetMix() => new()
    {
        ProductRoadmap = Quarter.ProductRoadmapPercent,
        TechRoadmap = Quarter.TechRoadmapPercent,
        Maintenance = Quarter.MaintenancePercent
    };

    /// <summary>Shares of a total by work type, in whole percent adding up to 100.</summary>
    public static MixSharesDto Shares(IReadOnlyDictionary<WorkType, decimal> byType)
    {
        var total = byType.Values.Sum();
        if (total <= 0) return new MixSharesDto();

        int Pct(WorkType type) => (int)Math.Round((byType.TryGetValue(type, out var v) ? v : 0m) / total * 100m);
        var product = Pct(WorkType.ProductRoadmap);
        var tech = Pct(WorkType.TechRoadmap);
        return new MixSharesDto { ProductRoadmap = product, TechRoadmap = tech, Maintenance = Math.Max(0, 100 - product - tech) };
    }

    // -----------------------------------------------------------------------------------------
    // Draft
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Proposes allocations for the cells the plan leaves free. Options go first, in priority
    /// order, while they are above the waterline or committed: their lead, then contributors,
    /// then the people who know the product best, from the option's earliest sprint, until its
    /// size is covered, never taking a sprint's lanes' share. What is left goes to the lanes,
    /// towards the target mix. Nothing already planned is changed.
    /// </summary>
    public IReadOnlyList<Allocation> Draft()
    {
        var free = new Dictionary<(Guid, Guid), int>();
        foreach (var r in Roster)
        {
            foreach (var s in Sprints)
            {
                free[(r.Person.Id, s.Id)] = Supply(r.Person.Id, s.Id) > 0 ? Math.Max(0, 100 - AllocatedPercent(r.Person.Id, s.Id)) : 0;
            }
        }

        var laneShare = (Quarter.MaintenancePercent + Quarter.TechRoadmapPercent) / 100m;
        var optionBudget = Sprints.ToDictionary(
            s => s.Id,
            s => SprintSupply(s.Id) * (1 - laneShare) - Allocations
                .Where(a => a.SprintId == s.Id && _initiatives.TryGetValue(a.InitiativeId, out var i) && !i.IsBucket)
                .Sum(Effort));

        var drafted = new List<Allocation>();
        var order = SprintOrder();
        var waterline = Waterline().Rows.ToDictionary(r => r.InitiativeId);

        foreach (var option in PriorityOrder())
        {
            if (option.Status is InitiativeStatus.Delivered) continue;
            var onLine = waterline.TryGetValue(option.Id, out var row) && row.AboveLine;
            if (!onLine && option.Status != InitiativeStatus.Committed) continue;

            var estimate = Estimate(option);
            if (estimate is null) continue;

            var remaining = estimate.Value - AllocatedEffort(option.Id) - drafted.Where(a => a.InitiativeId == option.Id).Sum(Effort);
            if (remaining <= 0) continue;

            var people = DraftPeople(option, free);
            var start = option.EarliestSprintId.HasValue && order.TryGetValue(option.EarliestSprintId.Value, out var e) ? e : 0;

            foreach (var sprint in Sprints.Skip(start))
            {
                foreach (var personId in people)
                {
                    if (remaining <= 0.005m) break;

                    var unit = Supply(personId, sprint.Id) / 100m;
                    var available = free.GetValueOrDefault((personId, sprint.Id));
                    if (unit <= 0 || available < DraftStep) continue;

                    var needed = RoundUpToStep((int)Math.Ceiling(remaining / unit));
                    var budget = RoundDownToStep((int)Math.Floor(Math.Max(0, optionBudget[sprint.Id]) / unit));
                    var percent = Math.Min(RoundDownToStep(available), Math.Min(needed, budget));
                    if (percent < DraftStep) continue;

                    var allocation = new Allocation(option.Id, personId, sprint.Id, percent);
                    drafted.Add(allocation);
                    free[(personId, sprint.Id)] = available - percent;
                    remaining -= percent * unit;
                    optionBudget[sprint.Id] -= percent * unit;
                }

                if (remaining <= 0.005m) break;
            }
        }

        drafted.AddRange(DraftLanes(free));
        return drafted;
    }

    private IReadOnlyList<Guid> DraftPeople(Initiative option, IReadOnlyDictionary<(Guid, Guid), int> free)
    {
        var members = Members
            .Where(m => m.InitiativeId == option.Id && IsOnRoster(m.DirectReportId))
            .OrderByDescending(m => m.IsLead)
            .ThenByDescending(m => Knowledge.Level(option.Id, m.DirectReportId))
            .Select(m => m.DirectReportId)
            .ToList();
        if (members.Count > 0) return members;

        // Nobody named yet: the two people who know the product best, or failing that, whoever
        // has the most time free
        var ranked = Roster
            .Select(r => r.Person.Id)
            .Select(id => (Id: id,
                Level: Knowledge.Level(option.Id, id),
                Tasks: Knowledge.TaskCount(option.Id, id),
                Free: Sprints.Sum(s => free.GetValueOrDefault((id, s.Id)))))
            .ToList();

        var knowing = ranked
            .Where(p => p.Level > 0 || p.Tasks > 0)
            .OrderByDescending(p => p.Level).ThenByDescending(p => p.Tasks).ThenByDescending(p => p.Free)
            .Take(2)
            .Select(p => p.Id)
            .ToList();

        return knowing.Count > 0
            ? knowing
            : ranked.OrderByDescending(p => p.Free).Take(2).Select(p => p.Id).ToList();
    }

    private IEnumerable<Allocation> DraftLanes(Dictionary<(Guid, Guid), int> free)
    {
        var lanes = Initiatives.Where(i => i.IsBucket).ToList();
        if (lanes.Count == 0) yield break;

        foreach (var sprint in Sprints)
        {
            var supply = SprintSupply(sprint.Id);
            var target = lanes.ToDictionary(
                l => l.Id,
                l => supply * Quarter.GetMixPercent(l.WorkType) / 100m -
                     Allocations.Where(a => a.InitiativeId == l.Id && a.SprintId == sprint.Id).Sum(Effort));

            foreach (var entry in Roster)
            {
                var personId = entry.Person.Id;
                var unit = Supply(personId, sprint.Id) / 100m;
                var available = RoundDownToStep(free.GetValueOrDefault((personId, sprint.Id)));
                while (unit > 0 && available >= DraftStep)
                {
                    var lane = lanes.OrderByDescending(l => target[l.Id]).First();

                    // To the nearest step, so the lanes land near their share rather than above it
                    var percent = Math.Min(available, RoundToStep(target[lane.Id] / unit));
                    if (percent < DraftStep) break;
                    yield return new Allocation(lane.Id, personId, sprint.Id, percent);
                    available -= percent;
                    free[(personId, sprint.Id)] -= percent;
                    target[lane.Id] -= percent * unit;
                }
            }
        }
    }

    private IReadOnlyList<Leave> LeavesOf(Guid personId) =>
        _leaves.TryGetValue(personId, out var leaves) ? leaves : Array.Empty<Leave>();

    private static int RoundUpToStep(int percent) => Math.Min(100, (percent + DraftStep - 1) / DraftStep * DraftStep);

    private static int RoundToStep(decimal percent) =>
        Math.Min(100, (int)Math.Round(percent / DraftStep, MidpointRounding.AwayFromZero) * DraftStep);

    private static int RoundDownToStep(int percent) => Math.Max(0, percent / DraftStep * DraftStep);

    public static decimal Round(decimal value) => Math.Round(value, 2);
}
