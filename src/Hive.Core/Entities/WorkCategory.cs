namespace Hive.Core.Entities;

/// <summary>
/// The kind of work a task is, decided by the name of the epic it rolls up into.
/// </summary>
public enum WorkCategory
{
    Productisation,
    TechMaintenance,
    TechRoadmap,
    TechDebt,
    SystemImprovements
}

/// <summary>
/// Decides a task's <see cref="WorkCategory"/>: its epic is the topmost parent it rolls up
/// into, and the epic's name names the category. Every other task, including one with no
/// epic, is productisation.
/// </summary>
public class WorkCategories
{
    /// <summary>
    /// The epic names that name each category until the manager sets their own
    /// (<see cref="AppSettings.GetWorkCategoryEpicNames"/>). Productisation is every other epic.
    /// </summary>
    public static readonly IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>> DefaultEpicNames =
        new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechMaintenance] = ["Tech Maintenance"],
            [WorkCategory.TechRoadmap] = ["Tech Roadmap"],
            [WorkCategory.TechDebt] = ["Tech Debt"],
            [WorkCategory.SystemImprovements] = ["System Improvement"]
        };

    private static readonly IReadOnlyList<(string Name, WorkCategory Category)> DefaultMatchers = Matchers(DefaultEpicNames);

    // A parent chain longer than this is a cycle in bad data, not a real hierarchy
    private const int MaxDepth = 10;

    private readonly Dictionary<Guid, Parent> _parents;
    private readonly Dictionary<Guid, TeamTask> _tasksById;
    private readonly Dictionary<string, TeamTask> _tasksByJiraKey;
    private readonly IReadOnlyList<(string Name, WorkCategory Category)> _matchers;

    /// <param name="parents">Every parent.</param>
    /// <param name="tasks">Every task, including the tasks that are parents themselves.</param>
    /// <param name="epicNames">The names that name each category; the defaults when null.</param>
    public WorkCategories(
        IEnumerable<Parent> parents,
        IEnumerable<TeamTask> tasks,
        IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>>? epicNames = null)
    {
        _matchers = epicNames is null ? DefaultMatchers : Matchers(epicNames);
        _parents = parents.ToDictionary(p => p.Id);
        var taskList = tasks.ToList();
        _tasksById = taskList.ToDictionary(t => t.Id);
        _tasksByJiraKey = new Dictionary<string, TeamTask>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in taskList)
        {
            var key = task.GetJiraKey();
            if (key != null)
            {
                _tasksByJiraKey.TryAdd(key, task);
            }
        }
    }

    /// <summary>
    /// The category whose name the epic's name contains, compared on letters and digits only,
    /// ignoring case: "Tech Debt 2026" and "TECH-DEBT" are Tech Debt. When names of two
    /// categories fit, the longest wins. Productisation for any other name.
    /// </summary>
    public static WorkCategory FromEpicName(
        string? name,
        IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>>? epicNames = null) =>
        Match(name, epicNames is null ? DefaultMatchers : Matchers(epicNames));

    /// <summary>
    /// A name as it is compared: its letters and digits, lower case.
    /// </summary>
    public static string Normalize(string name) =>
        new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static WorkCategory Match(string? name, IReadOnlyList<(string Name, WorkCategory Category)> matchers)
    {
        if (string.IsNullOrWhiteSpace(name))
            return WorkCategory.Productisation;

        var letters = Normalize(name);
        foreach (var (epicName, category) in matchers)
        {
            if (letters.Contains(epicName, StringComparison.Ordinal))
                return category;
        }

        return WorkCategory.Productisation;
    }

    // Longest first, so the most specific name wins
    private static IReadOnlyList<(string Name, WorkCategory Category)> Matchers(
        IReadOnlyDictionary<WorkCategory, IReadOnlyList<string>> epicNames) =>
        epicNames
            .Where(entry => entry.Key != WorkCategory.Productisation)
            .SelectMany(entry => entry.Value.Select(name => (Name: Normalize(name), Category: entry.Key)))
            .Where(matcher => matcher.Name.Length > 0)
            .OrderByDescending(matcher => matcher.Name.Length)
            .ToList();

    /// <summary>
    /// The epic a task rolls up into: the parent of its parent's task, and so on, up to a
    /// parent that is a Jira epic or whose own task has no parent. Null when the task has no
    /// parent.
    /// </summary>
    public Parent? FindEpic(TeamTask task)
    {
        Parent? current = null;
        var parentId = task.ParentId;
        for (var depth = 0; depth < MaxDepth && parentId.HasValue; depth++)
        {
            if (!_parents.TryGetValue(parentId.Value, out var parent))
                break;

            current = parent;
            if (string.Equals(parent.JiraIssueType, "Epic", StringComparison.OrdinalIgnoreCase))
                break;

            parentId = TaskOf(parent)?.ParentId;
        }

        return current;
    }

    /// <summary>
    /// The task's category, from the name of its epic.
    /// </summary>
    public WorkCategory For(TeamTask task) => Match(FindEpic(task)?.Name, _matchers);

    private TeamTask? TaskOf(Parent parent)
    {
        if (parent.TeamTaskId.HasValue && _tasksById.TryGetValue(parent.TeamTaskId.Value, out var task))
            return task;

        return parent.JiraKey != null && _tasksByJiraKey.TryGetValue(parent.JiraKey, out var byKey) ? byKey : null;
    }
}
