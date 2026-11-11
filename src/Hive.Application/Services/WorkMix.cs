using Hive.Application.DTOs;
using Hive.Core.Entities;

namespace Hive.Application.Services;

/// <summary>
/// How the team's work splits by kind, in tasks and story points: the Dashboard's Work mix and
/// the starting target for a quarter's mix both read it from here.
/// </summary>
public static class WorkMix
{
    /// <summary>
    /// The tasks that count: a task that is also a parent (an epic or story with children) is
    /// counted through its children instead.
    /// </summary>
    public static List<TeamTask> ExcludeParentTasks(IReadOnlyList<TeamTask> tasks, IReadOnlyList<Parent> parents)
    {
        var parentNames = parents.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tasks.Where(t => !parentNames.Contains(t.Title)).ToList();
    }

    /// <summary>
    /// A task's story points for the sprint it is in: its points less any already delivered in
    /// earlier sprints, so work carried over is not counted twice.
    /// </summary>
    public static int NewStoryPoints(TeamTask task) =>
        task.PreviousSprintsStoryPoints.HasValue && task.StoryPoints.HasValue
            ? Math.Max(0, task.StoryPoints.Value - task.PreviousSprintsStoryPoints.Value)
            : task.StoryPoints ?? 0;

    /// <summary>Each work category's share of the tasks and of their story points, every category listed.</summary>
    public static IReadOnlyList<WorkCategoryShareDto> ByCategory(IReadOnlyList<TeamTask> tasks, WorkCategories workCategories)
    {
        var totalStoryPoints = tasks.Sum(NewStoryPoints);
        var tasksByCategory = tasks.ToLookup(workCategories.For);
        return Enum.GetValues<WorkCategory>()
            .Select(category =>
            {
                var categoryTasks = tasksByCategory[category].ToList();
                var storyPoints = categoryTasks.Sum(NewStoryPoints);
                return new WorkCategoryShareDto
                {
                    Category = category,
                    CategoryName = CategoryName(category),
                    TaskCount = categoryTasks.Count,
                    StoryPoints = storyPoints,
                    PercentageOfTasks = tasks.Count > 0
                        ? Math.Round((double)categoryTasks.Count / tasks.Count * 100, 1)
                        : 0,
                    PercentageOfStoryPoints = totalStoryPoints > 0
                        ? Math.Round((double)storyPoints / totalStoryPoints * 100, 1)
                        : 0
                };
            })
            .ToList();
    }

    /// <summary>
    /// The work categories as a quarter's mix, by story points: productisation and system
    /// improvements are the product roadmap, tech roadmap and tech debt the tech roadmap, and tech
    /// maintenance is maintenance. Null when there are no story points.
    /// </summary>
    public static MixSharesDto? AsPlanningMix(IReadOnlyList<WorkCategoryShareDto> shares)
    {
        if (shares.Sum(s => s.StoryPoints) <= 0) return null;

        return QuarterPlan.Shares(shares
            .GroupBy(s => WorkTypeOf(s.Category))
            .ToDictionary(g => g.Key, g => (decimal)g.Sum(s => s.StoryPoints)));
    }

    private static WorkType WorkTypeOf(WorkCategory category) => category switch
    {
        WorkCategory.TechMaintenance => WorkType.Maintenance,
        WorkCategory.TechRoadmap or WorkCategory.TechDebt => WorkType.TechRoadmap,
        _ => WorkType.ProductRoadmap
    };

    private static string CategoryName(WorkCategory category) => category switch
    {
        WorkCategory.TechMaintenance => "Tech Maintenance",
        WorkCategory.TechRoadmap => "Tech Roadmap",
        WorkCategory.TechDebt => "Tech Debt",
        WorkCategory.SystemImprovements => "System Improvements",
        _ => "Productisation"
    };
}
