using Hive.Application.DTOs;
using Hive.Application.Formatting;
using Hive.Core.Entities;

namespace Hive.Application.Services;

/// <summary>
/// What a quarter's plan should warn about, read from the plan as it stands. Critical first,
/// then warnings, then information.
/// </summary>
internal static class PlanningInsights
{
    /// <summary>A lead is flagged when they are around for less than this share of a sprint they lead in.</summary>
    public const decimal LeadAvailabilityFloor = 0.5m;

    /// <summary>Leading this many initiatives at once is too many for one person.</summary>
    public const int MaxLeadsPerPerson = 3;

    /// <summary>A lead should know the code at least this well (1–5).</summary>
    public const int MinLeadKnowledge = 3;

    /// <summary>A work type's planned share this far from its target, in points, is off target.</summary>
    public const int MixTolerance = 10;

    public static IReadOnlyList<PlanningInsightDto> Build(
        QuarterPlan plan,
        IReadOnlyList<ExternalDependency> externalDependencies,
        IReadOnlyList<InitiativeDependency> dependencies)
    {
        var insights = new List<PlanningInsightDto>();
        var waterline = plan.Waterline();
        var aboveLine = waterline.Rows.Where(r => r.AboveLine).Select(r => r.InitiativeId).ToHashSet();
        var names = plan.Roster.ToDictionary(r => r.Person.Id, r => r.Person.FullName);
        var order = plan.SprintOrder();

        // Options the plan takes on: committed ones, and, before anything is committed, those above the line
        var anyCommitted = plan.Initiatives.Any(i => !i.IsBucket && i.Status == InitiativeStatus.Committed);
        var takenOn = plan.PriorityOrder()
            .Where(i => i.Status == InitiativeStatus.Committed || (!anyCommitted && aboveLine.Contains(i.Id) && i.Status != InitiativeStatus.Delivered))
            .ToList();

        foreach (var size in plan.Initiatives
                     .Where(i => !i.IsBucket && i.IsInPlay && i.TshirtSize is not null && plan.Estimate(i) is null)
                     .GroupBy(i => i.TshirtSize))
        {
            insights.Add(new PlanningInsightDto
            {
                Type = "UnknownSize",
                Severity = InsightSeverity.Warning,
                Title = $"No effort for size {size.Key}",
                Message = $"{string.Join(", ", size.Select(i => i.Name))}: size {size.Key} has no mapping in Settings, so it counts as nothing on the waterline.",
                InitiativeId = size.First().Id
            });
        }

        foreach (var option in plan.PriorityOrder().Where(i => i.Status is InitiativeStatus.ReadyForCommitment or InitiativeStatus.Committed or InitiativeStatus.Stretch && i.TshirtSize is null))
        {
            insights.Add(new PlanningInsightDto
            {
                Type = "Unsized",
                Severity = option.Status == InitiativeStatus.Committed ? InsightSeverity.Warning : InsightSeverity.Info,
                Title = $"{option.Name} has no size",
                Message = "It is ready for commitment but not sized, so it cannot fit under the waterline. Give it a T-shirt size.",
                InitiativeId = option.Id
            });
        }

        var committedEffort = waterline.CommittedEffort;
        if (committedEffort > waterline.Capacity + 0.005m)
        {
            insights.Add(new PlanningInsightDto
            {
                Type = "OverCommitted",
                Severity = InsightSeverity.Critical,
                Title = "Committed beyond capacity",
                Message = $"Committed options need {committedEffort:0.#} dev-sprints; after the lanes' share the team has {waterline.Capacity:0.#}."
            });
        }

        foreach (var option in plan.PriorityOrder().Where(i => i.Status == InitiativeStatus.Committed && !aboveLine.Contains(i.Id)))
        {
            insights.Add(new PlanningInsightDto
            {
                Type = "CommittedBelowLine",
                Severity = InsightSeverity.Warning,
                Title = $"{option.Name} is committed below the line",
                Message = "Its place in the priority order does not fit the capacity for options. Raise it, or make it a stretch.",
                InitiativeId = option.Id
            });
        }

        foreach (var option in takenOn)
        {
            if (option.HasReadinessIssues)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "NotReady",
                    Severity = option.Status == InitiativeStatus.Committed ? InsightSeverity.Critical : InsightSeverity.Warning,
                    Title = $"{option.Name} is not ready",
                    Message = $"Still open: {DescribeReadiness(option.Readiness)}.",
                    InitiativeId = option.Id
                });
            }

            var open = externalDependencies.Where(d => d.InitiativeId == option.Id && d.IsOpen).ToList();
            if (open.Count > 0)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "OpenDependency",
                    Severity = open.Any(d => d.Status == ExternalDependencyStatus.AtRisk) ? InsightSeverity.Critical : InsightSeverity.Warning,
                    Title = $"{option.Name} depends on {string.Join(", ", open.Select(d => d.Team).Distinct())}",
                    Message = string.Join("; ", open.Select(d => $"{d.Team}: {(d.Status == ExternalDependencyStatus.AtRisk ? "at risk" : "not agreed yet")}{(string.IsNullOrEmpty(d.Need) ? "" : $" ({d.Need})")}")),
                    InitiativeId = option.Id
                });
            }

            var members = plan.Members.Where(m => m.InitiativeId == option.Id).ToList();
            var lead = members.FirstOrDefault(m => m.IsLead);
            if (lead is null)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "NoLead",
                    Severity = InsightSeverity.Warning,
                    Title = $"{option.Name} has no lead",
                    Message = "Name who leads it. The suggestions come from the knowledge matrix and who has worked on its products.",
                    InitiativeId = option.Id
                });
            }
            else if (plan.Knowledge.HasProjects(option.Id) && plan.Knowledge.Level(option.Id, lead.DirectReportId) < MinLeadKnowledge)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "LeadKnowledge",
                    Severity = InsightSeverity.Info,
                    Title = $"{Name(names, lead.DirectReportId)} leads {option.Name} with little knowledge of it",
                    Message = $"Their knowledge of its projects is {plan.Knowledge.Level(option.Id, lead.DirectReportId)} of 5. Pair them with someone who knows it.",
                    InitiativeId = option.Id,
                    DirectReportId = lead.DirectReportId
                });
            }

            if (lead is not null)
            {
                foreach (var sprint in plan.Sprints)
                {
                    var leads = plan.Allocations.Any(a => a.InitiativeId == option.Id && a.DirectReportId == lead.DirectReportId && a.SprintId == sprint.Id);
                    if (leads && plan.Availability(lead.DirectReportId, sprint.Id) < LeadAvailabilityFloor)
                    {
                        insights.Add(new PlanningInsightDto
                        {
                            Type = "LeadAway",
                            Severity = InsightSeverity.Warning,
                            Title = $"{option.Name}'s lead is away in {sprint.Name}",
                            Message = $"{Name(names, lead.DirectReportId)} is off {plan.LeaveDays(lead.DirectReportId, sprint.Id)} working days of that sprint.",
                            InitiativeId = option.Id,
                            DirectReportId = lead.DirectReportId,
                            SprintId = sprint.Id
                        });
                    }
                }
            }

            var forecast = plan.Forecast(option);
            if (forecast.Allocated <= 0)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "Unallocated",
                    Severity = option.Status == InitiativeStatus.Committed ? InsightSeverity.Warning : InsightSeverity.Info,
                    Title = $"Nobody is planned on {option.Name}",
                    Message = "Give it people in the allocation grid, or let Draft plan do it.",
                    InitiativeId = option.Id
                });
            }
            else if (forecast.Estimate.HasValue && !forecast.DeliveredInQuarter)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "NotDelivered",
                    Severity = InsightSeverity.Warning,
                    Title = $"{option.Name} will not be delivered this quarter",
                    Message = $"The plan covers {forecast.Allocated:0.#} of {forecast.Estimate:0.#} dev-sprints ({forecast.CoveredPercent}% of {option.TshirtSize}).",
                    InitiativeId = option.Id
                });
            }

            if (option.DueDate.HasValue && forecast.DoneSprintId.HasValue)
            {
                var doneSprint = plan.Sprints.First(s => s.Id == forecast.DoneSprintId);
                if (doneSprint.EndDate.HasValue && doneSprint.EndDate.Value.Date > option.DueDate.Value.Date)
                {
                    insights.Add(new PlanningInsightDto
                    {
                        Type = "PastDueDate",
                        Severity = InsightSeverity.Warning,
                        Title = $"{option.Name} finishes after its due date",
                        Message = $"Due {option.DueDate.Value.ToDisplayDate()}; the plan finishes it in {doneSprint.Name}, ending {doneSprint.EndDate.Value.ToDisplayDate()}.",
                        InitiativeId = option.Id,
                        SprintId = doneSprint.Id
                    });
                }
            }
        }

        foreach (var dependency in dependencies)
        {
            var dependent = plan.Initiatives.FirstOrDefault(i => i.Id == dependency.DependentInitiativeId);
            var needed = plan.Initiatives.FirstOrDefault(i => i.Id == dependency.DependencyInitiativeId);
            if (dependent is null || needed is null || !dependent.IsInPlay) continue;

            if (needed.Status == InitiativeStatus.Dropped)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "DependencyDropped",
                    Severity = InsightSeverity.Critical,
                    Title = $"{dependent.Name} depends on dropped {needed.Name}",
                    Message = "The work it needs is not in the plan any more.",
                    InitiativeId = dependent.Id
                });
                continue;
            }

            var starts = plan.Forecast(dependent).FirstSprintId;
            var neededDone = plan.Forecast(needed);
            var neededLast = plan.Allocations.Where(a => a.InitiativeId == needed.Id).Select(a => order.GetValueOrDefault(a.SprintId, -1)).DefaultIfEmpty(-1).Max();
            if (starts.HasValue && order.TryGetValue(starts.Value, out var startIndex) && neededLast >= 0 && startIndex <= neededLast)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "DependencyOrder",
                    Severity = InsightSeverity.Warning,
                    Title = $"{dependent.Name} starts before {needed.Name} is finished",
                    Message = $"It starts in {plan.Sprints[startIndex].Name}, while {needed.Name} is planned until {plan.Sprints[neededLast].Name}{(neededDone.DeliveredInQuarter ? "" : " and is not finished this quarter")}.",
                    InitiativeId = dependent.Id
                });
            }
        }

        foreach (var entry in plan.Roster)
        {
            foreach (var sprint in plan.Sprints)
            {
                if (plan.AllocatedPercent(entry.Person.Id, sprint.Id) > 0 &&
                    plan.Availability(entry.Person.Id, sprint.Id) == 0 &&
                    plan.LeaveDays(entry.Person.Id, sprint.Id) > 0)
                {
                    insights.Add(new PlanningInsightDto
                    {
                        Type = "AllocatedOnLeave",
                        Severity = InsightSeverity.Warning,
                        Title = $"{entry.Person.FullName} is planned in {sprint.Name} but away all of it",
                        Message = "Their allocation there is worth nothing. Move it to someone else.",
                        DirectReportId = entry.Person.Id,
                        SprintId = sprint.Id
                    });
                }
            }

            var leading = plan.Members
                .Where(m => m.DirectReportId == entry.Person.Id && m.IsLead)
                .Select(m => plan.Initiatives.FirstOrDefault(i => i.Id == m.InitiativeId))
                .Where(i => i is not null && i.IsInPlay && i.Status != InitiativeStatus.Delivered)
                .ToList();
            if (leading.Count >= MaxLeadsPerPerson)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "LeadOverload",
                    Severity = InsightSeverity.Warning,
                    Title = $"{entry.Person.FullName} leads {leading.Count} initiatives",
                    Message = string.Join(", ", leading.Select(i => i!.Name)) + ". Spread the leads, so one absence does not stall them all.",
                    DirectReportId = entry.Person.Id
                });
            }
        }

        if (plan.Allocations.Count > 0)
        {
            var planned = plan.PlannedMix();
            var target = plan.TargetMix();
            var off = new List<string>();
            void Check(string label, int plannedShare, int targetShare)
            {
                if (Math.Abs(plannedShare - targetShare) >= MixTolerance)
                {
                    off.Add($"{label} {plannedShare}% against {targetShare}%");
                }
            }

            Check("product roadmap", planned.ProductRoadmap, target.ProductRoadmap);
            Check("tech roadmap", planned.TechRoadmap, target.TechRoadmap);
            Check("maintenance", planned.Maintenance, target.Maintenance);
            if (off.Count > 0)
            {
                insights.Add(new PlanningInsightDto
                {
                    Type = "MixOffTarget",
                    Severity = InsightSeverity.Info,
                    Title = "The mix is off target",
                    Message = $"Planned {string.Join(", ", off)}."
                });
            }
        }

        return insights
            .OrderByDescending(i => i.Severity)
            .ToList();
    }

    public static string DescribeReadiness(ReadinessIssue issues)
    {
        var parts = new List<string>();
        if (issues.HasFlag(ReadinessIssue.RequirementsPending)) parts.Add("product requirements");
        if (issues.HasFlag(ReadinessIssue.EstimationPending)) parts.Add("estimation");
        if (issues.HasFlag(ReadinessIssue.SolutionDesignPending)) parts.Add("solution design");
        if (issues.HasFlag(ReadinessIssue.ReleaseCoordination)) parts.Add("release coordination");
        return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
    }

    private static string Name(IReadOnlyDictionary<Guid, string> names, Guid id) =>
        names.TryGetValue(id, out var name) ? name : "Someone";
}
