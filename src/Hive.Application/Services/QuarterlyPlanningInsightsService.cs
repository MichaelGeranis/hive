using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service for generating quarterly planning insights.
/// </summary>
public class QuarterlyPlanningInsightsService : IQuarterlyPlanningInsightsService
{
    private readonly IQuarterRepository _quarterRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly IInitiativeDependencyRepository _dependencyRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly ILeaveRepository _leaveRepository;

    private const int BottleneckInitiativeThreshold = 3; // 3+ initiatives in one sprint

    public QuarterlyPlanningInsightsService(
        IQuarterRepository quarterRepository,
        IInitiativeRepository initiativeRepository,
        IAllocationRepository allocationRepository,
        IInitiativeDependencyRepository dependencyRepository,
        ISprintRepository sprintRepository,
        IDirectReportRepository directReportRepository,
        ILeaveRepository leaveRepository)
    {
        _quarterRepository = quarterRepository ?? throw new ArgumentNullException(nameof(quarterRepository));
        _initiativeRepository = initiativeRepository ?? throw new ArgumentNullException(nameof(initiativeRepository));
        _allocationRepository = allocationRepository ?? throw new ArgumentNullException(nameof(allocationRepository));
        _dependencyRepository = dependencyRepository ?? throw new ArgumentNullException(nameof(dependencyRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _leaveRepository = leaveRepository ?? throw new ArgumentNullException(nameof(leaveRepository));
    }

    public async Task<PlanningInsightsDto> GenerateInsightsAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarter = await _quarterRepository.GetByIdAsync(quarterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quarter), quarterId);

        // Load all required data
        var initiatives = await _initiativeRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var allocations = await _allocationRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var dependencies = await _dependencyRepository.GetByQuarterAsync(quarterId, cancellationToken);
        var sprints = await _sprintRepository.GetByYearQuarterAsync(quarter.Year, quarter.QuarterNumber, cancellationToken);
        var allMembers = await _directReportRepository.GetAllAsync(cancellationToken);
        var directReports = allMembers.Where(d => d.IsDirect).ToList();
        var leaves = await _leaveRepository.GetAllAsync(cancellationToken);

        // Calculate summary metrics
        var allocatedInitiativeIds = allocations.Select(a => a.InitiativeId).Distinct().ToHashSet();
        var allocatedInitiatives = initiatives.Count(i => allocatedInitiativeIds.Contains(i.Id));

        // Generate insights
        var insights = new List<PlanningInsightDto>();

        // 1. Leave Conflict Alerts
        insights.AddRange(DetectLeaveConflicts(allocations, leaves, sprints, directReports));

        // 2. Dependency Risk Identification
        insights.AddRange(DetectDependencyRisks(dependencies, allocations, initiatives, sprints));

        // 3. Bottleneck Detection
        insights.AddRange(DetectBottlenecks(allocations, directReports, sprints));

        // 4. Unassigned Work
        insights.AddRange(DetectUnassignedWork(initiatives, allocations));

        // Generate team member summaries
        var teamMemberSummaries = GenerateTeamMemberSummaries(allocations, directReports, sprints, leaves);

        return new PlanningInsightsDto
        {
            TotalInitiatives = initiatives.Count,
            AllocatedInitiatives = allocatedInitiatives,
            IssueCount = insights.Count(i => i.Severity == InsightSeverity.Info),
            WarningCount = insights.Count(i => i.Severity == InsightSeverity.Warning),
            CriticalCount = insights.Count(i => i.Severity == InsightSeverity.Critical),
            Insights = insights,
            TeamMemberSummaries = teamMemberSummaries
        };
    }

    private static IEnumerable<PlanningInsightDto> DetectLeaveConflicts(
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<Leave> leaves,
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<DirectReport> directReports)
    {
        var memberNames = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var sprintNames = sprints.ToDictionary(s => s.Id, s => s.Name);

        foreach (var allocation in allocations)
        {
            var sprint = sprints.FirstOrDefault(s => s.Id == allocation.SprintId);
            if (sprint?.StartDate == null || sprint.EndDate == null) continue;

            var conflictingLeaves = leaves.Where(l =>
                l.DirectReportId == allocation.DirectReportId &&
                l.Status == LeaveStatus.Active &&
                l.StartDate <= sprint.EndDate &&
                l.EndDate >= sprint.StartDate);

            foreach (var leave in conflictingLeaves)
            {
                memberNames.TryGetValue(allocation.DirectReportId, out var memberName);
                sprintNames.TryGetValue(allocation.SprintId, out var sprintName);

                yield return new PlanningInsightDto
                {
                    Type = InsightType.LeaveConflict,
                    Severity = InsightSeverity.Warning,
                    Title = "Leave Conflict",
                    Message = $"{memberName ?? "Team member"} is on leave during {sprintName ?? "sprint"} ({leave.StartDate:MMM d} - {leave.EndDate:MMM d})",
                    RelatedDirectReportId = allocation.DirectReportId,
                    RelatedSprintId = allocation.SprintId,
                    AffectedCells = new[] { allocation.Id }
                };
            }
        }
    }

    private static IEnumerable<PlanningInsightDto> DetectDependencyRisks(
        IReadOnlyList<InitiativeDependency> dependencies,
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<Initiative> initiatives,
        IReadOnlyList<Sprint> sprints)
    {
        var initiativeNames = initiatives.ToDictionary(i => i.Id, i => i.Name);
        var sprintOrder = sprints.ToDictionary(s => s.Id, s => s.GetSortOrder());

        // Group allocations by initiative to find earliest sprint
        var earliestSprintByInitiative = allocations
            .GroupBy(a => a.InitiativeId)
            .ToDictionary(
                g => g.Key,
                g => g.Min(a => sprintOrder.GetValueOrDefault(a.SprintId, int.MaxValue)));

        var latestSprintByInitiative = allocations
            .GroupBy(a => a.InitiativeId)
            .ToDictionary(
                g => g.Key,
                g => g.Max(a => sprintOrder.GetValueOrDefault(a.SprintId, 0)));

        foreach (var dependency in dependencies.Where(d => d.Type == DependencyType.FinishToStart))
        {
            earliestSprintByInitiative.TryGetValue(dependency.DependentInitiativeId, out var dependentStart);
            latestSprintByInitiative.TryGetValue(dependency.DependencyInitiativeId, out var dependencyEnd);

            // If dependent starts before/same time as dependency ends, there's a risk
            if (dependentStart > 0 && dependencyEnd > 0 && dependentStart <= dependencyEnd)
            {
                initiativeNames.TryGetValue(dependency.DependentInitiativeId, out var dependentName);
                initiativeNames.TryGetValue(dependency.DependencyInitiativeId, out var dependencyName);

                yield return new PlanningInsightDto
                {
                    Type = InsightType.DependencyRisk,
                    Severity = InsightSeverity.Critical,
                    Title = "Dependency Risk",
                    Message = $"'{dependentName}' is scheduled to start before '{dependencyName}' completes",
                    RelatedInitiativeId = dependency.DependentInitiativeId
                };
            }
        }
    }

    private static IEnumerable<PlanningInsightDto> DetectBottlenecks(
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Sprint> sprints)
    {
        var memberNames = directReports.ToDictionary(d => d.Id, d => d.FullName);
        var sprintNames = sprints.ToDictionary(s => s.Id, s => s.Name);

        var allocationsByMemberSprint = allocations
            .GroupBy(a => new { a.DirectReportId, a.SprintId })
            .Select(g => new
            {
                g.Key.DirectReportId,
                g.Key.SprintId,
                InitiativeCount = g.Select(a => a.InitiativeId).Distinct().Count()
            });

        foreach (var group in allocationsByMemberSprint.Where(g => g.InitiativeCount >= BottleneckInitiativeThreshold))
        {
            memberNames.TryGetValue(group.DirectReportId, out var memberName);
            sprintNames.TryGetValue(group.SprintId, out var sprintName);

            yield return new PlanningInsightDto
            {
                Type = InsightType.Bottleneck,
                Severity = InsightSeverity.Warning,
                Title = "Potential Bottleneck",
                Message = $"{memberName ?? "Team member"} has {group.InitiativeCount} initiatives in {sprintName ?? "sprint"}",
                RelatedDirectReportId = group.DirectReportId,
                RelatedSprintId = group.SprintId
            };
        }
    }

    private static IEnumerable<PlanningInsightDto> DetectUnassignedWork(
        IReadOnlyList<Initiative> initiatives,
        IReadOnlyList<Allocation> allocations)
    {
        var allocatedInitiativeIds = allocations.Select(a => a.InitiativeId).Distinct().ToHashSet();

        foreach (var initiative in initiatives.Where(i =>
            i.Status != InitiativeStatus.Cancelled &&
            i.Status != InitiativeStatus.Completed &&
            !allocatedInitiativeIds.Contains(i.Id)))
        {
            yield return new PlanningInsightDto
            {
                Type = InsightType.UnassignedWork,
                Severity = InsightSeverity.Info,
                Title = "Unassigned Initiative",
                Message = $"'{initiative.Name}' has no allocations",
                RelatedInitiativeId = initiative.Id
            };
        }
    }

    private static IReadOnlyList<TeamMemberSummaryDto> GenerateTeamMemberSummaries(
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<Leave> leaves)
    {
        var summaries = new List<TeamMemberSummaryDto>();

        foreach (var member in directReports)
        {
            var memberAllocations = allocations.Where(a => a.DirectReportId == member.Id).ToList();
            var memberLeaves = leaves.Where(l => l.DirectReportId == member.Id && l.Status == LeaveStatus.Active).ToList();

            var sprintAllocations = sprints.Select(sprint =>
            {
                var sprintAllocs = memberAllocations.Where(a => a.SprintId == sprint.Id);
                var hasLeave = sprint.StartDate.HasValue && sprint.EndDate.HasValue &&
                    memberLeaves.Any(l => l.StartDate <= sprint.EndDate && l.EndDate >= sprint.StartDate);

                var leaveDays = 0;
                if (hasLeave && sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                {
                    var overlappingLeaves = memberLeaves.Where(l =>
                        l.StartDate <= sprint.EndDate && l.EndDate >= sprint.StartDate);
                    leaveDays = overlappingLeaves.Sum(l =>
                    {
                        var start = l.StartDate > sprint.StartDate.Value ? l.StartDate : sprint.StartDate.Value;
                        var end = l.EndDate < sprint.EndDate.Value ? l.EndDate : sprint.EndDate.Value;
                        return (int)(end - start).TotalDays + 1;
                    });
                }

                return new SprintAllocationSummary
                {
                    SprintId = sprint.Id,
                    AllocationCount = sprintAllocs.Count(),
                    HasLeave = hasLeave,
                    LeaveDays = leaveDays
                };
            }).ToList();

            summaries.Add(new TeamMemberSummaryDto
            {
                DirectReportId = member.Id,
                Name = member.FullName,
                InitiativeCount = memberAllocations.Select(a => a.InitiativeId).Distinct().Count(),
                SprintAllocations = sprintAllocations
            });
        }

        return summaries;
    }
}
