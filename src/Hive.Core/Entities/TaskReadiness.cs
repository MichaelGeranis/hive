using System.Security.Cryptography;
using System.Text;

namespace Hive.Core.Entities;

/// <summary>
/// What an AI review made of a task before it is built.
/// </summary>
public enum TaskReadinessVerdict
{
    /// <summary>Clear enough to build.</summary>
    Ready,

    /// <summary>Something important is missing or unclear.</summary>
    NeedsWork,

    /// <summary>Too little written to tell.</summary>
    Unclear
}

/// <summary>
/// Which tasks are checked before they are built, and what a task is, for its review.
/// </summary>
public static class TaskReadiness
{
    /// <summary>
    /// The current sprint (<see cref="Sprint.FindCurrent"/>) and the one after it. Either is
    /// null when Hive does not have it.
    /// </summary>
    public static (Sprint? Current, Sprint? Next) FindSprints(IEnumerable<Sprint> sprints, DateTime today)
    {
        var all = sprints.ToList();
        var current = Sprint.FindCurrent(all, today);
        if (current is null)
        {
            return (null, null);
        }

        var next = all
            .Where(s => s.IsAfter(current))
            .OrderBy(s => s.GetSortOrder())
            .FirstOrDefault();
        return (current, next);
    }

    /// <summary>
    /// The tasks to check before they are built: not started, not an epic, and planned for
    /// the current or the next sprint, by the latest sprint they list. Current sprint first,
    /// then by priority, highest first.
    /// </summary>
    public static IReadOnlyList<TeamTask> FindUpcoming(IEnumerable<TeamTask> tasks, IEnumerable<Sprint> sprints, DateTime today)
    {
        var all = sprints.ToList();
        var (current, next) = FindSprints(all, today);
        if (current is null)
        {
            return [];
        }

        var byName = all
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Sprint? LatestSprint(TeamTask task) => task.GetSprintNames()
            .Select(name => byName.GetValueOrDefault(name))
            .OfType<Sprint>()
            .OrderByDescending(s => s.GetSortOrder())
            .FirstOrDefault();

        return tasks
            .Where(t => t.IsNotStarted() && t.Type != TaskType.Epic)
            .Select(t => (Task: t, Sprint: LatestSprint(t)))
            .Where(x => x.Sprint is not null && (x.Sprint.Id == current.Id || x.Sprint.Id == next?.Id))
            .OrderBy(x => x.Sprint!.Id == current.Id ? 0 : 1)
            .ThenByDescending(x => x.Task.Priority)
            .ThenBy(x => x.Task.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Task)
            .ToList();
    }

    /// <summary>
    /// What a task is, for its review: its title, type and description. Changing any of them
    /// makes an earlier review out of date.
    /// </summary>
    public static string Fingerprint(TeamTask task)
    {
        var text = string.Join('\n',
            task.Title,
            task.Type.ToString(),
            task.Description);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>
    /// Reads a verdict by its name, ignoring case, spaces and punctuation, so "needs work"
    /// and "NeedsWork" are the same.
    /// </summary>
    public static bool TryParseVerdict(string? value, out TaskReadinessVerdict verdict)
    {
        var letters = new string((value ?? string.Empty).Where(char.IsLetter).ToArray());
        return Enum.TryParse(letters, ignoreCase: true, out verdict) && letters.Length > 0;
    }
}
