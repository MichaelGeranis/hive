using System.Text.RegularExpressions;

namespace Hive.Core.Entities;

/// <summary>
/// Represents a parent item that groups related tasks (e.g., Epics, Features).
/// A parent belongs to the products its Jira components name.
/// </summary>
public class Parent
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Labels { get; private set; } = string.Empty;
    public Guid? TeamTaskId { get; private set; }

    /// <summary>
    /// The key of the Jira epic this parent represents, e.g. <c>MAR-128</c>. Null for a
    /// parent created by hand or by an older CSV import. Once set, it is the parent's
    /// identity for Jira sync: the name can change in Jira without creating a new parent.
    /// </summary>
    public string? JiraKey { get; private set; }

    /// <summary>
    /// The Jira issue type of the parent, e.g. Epic, Story or Support. In Jira any issue
    /// with sub-tasks is a parent, not only an epic. Null when the parent is not from Jira.
    /// </summary>
    public string? JiraIssueType { get; private set; }

    /// <summary>
    /// The Jira components of the parent issue, comma-separated, e.g. <c>Matching,IPI</c>.
    /// Each names a <see cref="Product"/> the parent belongs to. Set only by the Jira sync;
    /// empty for a parent that is not from Jira or whose issue has no components.
    /// </summary>
    public string Components { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Parent() { }

    public Parent(string name, string? labels = null, Guid? teamTaskId = null)
    {
        ValidateName(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        Labels = labels?.Trim() ?? string.Empty;
        TeamTaskId = teamTaskId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? labels = null)
    {
        ValidateName(name);

        Name = name.Trim();
        Labels = labels?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates the parent for a Jira issue Hive has not seen before.
    /// </summary>
    public static Parent FromJira(string jiraKey, string issueType, string name, IEnumerable<string>? components = null)
    {
        var parent = new Parent(name);
        parent.JiraKey = NormalizeJiraKey(jiraKey);
        parent.JiraIssueType = NormalizeIssueType(issueType);
        parent.Components = NormalizeComponents(components);
        return parent;
    }

    /// <summary>
    /// Brings the parent in line with its Jira issue. Jira owns the name, the issue type and
    /// the components; the labels are the manager's and are left alone. A parent never
    /// changes to another Jira issue.
    /// </summary>
    public void SyncFromJira(string jiraKey, string issueType, string name, IEnumerable<string>? components = null)
    {
        var key = NormalizeJiraKey(jiraKey);
        var type = NormalizeIssueType(issueType);
        var normalizedComponents = NormalizeComponents(components);
        ValidateName(name);

        if (JiraKey is not null && JiraKey != key)
        {
            throw new InvalidOperationException($"Parent '{Name}' is linked to {JiraKey}, not {key}.");
        }

        JiraKey = key;
        JiraIssueType = type;
        Components = normalizedComponents;
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Whether syncing from Jira would change anything.
    /// </summary>
    public bool MatchesJira(string jiraKey, string issueType, string name, IEnumerable<string>? components = null) =>
        JiraKey == NormalizeJiraKey(jiraKey) &&
        JiraIssueType == NormalizeIssueType(issueType) &&
        Components == NormalizeComponents(components) &&
        Name == name.Trim();

    public void LinkToTask(Guid teamTaskId)
    {
        TeamTaskId = teamTaskId;
        UpdatedAt = DateTime.UtcNow;
    }

    private static readonly Regex JiraKeyPattern = new(@"^[A-Z][A-Z0-9_]*-[1-9][0-9]*$", RegexOptions.Compiled);

    private static string NormalizeJiraKey(string jiraKey)
    {
        var key = jiraKey?.Trim().ToUpperInvariant() ?? string.Empty;
        if (key.Length > 50 || !JiraKeyPattern.IsMatch(key))
        {
            throw new ArgumentException($"'{jiraKey}' is not a Jira issue key such as MAR-128.", nameof(jiraKey));
        }

        return key;
    }

    private static string NormalizeIssueType(string issueType)
    {
        var type = issueType?.Trim() ?? string.Empty;
        if (type.Length == 0 || type.Length > 50)
        {
            throw new ArgumentException("A Jira issue type must be 1 to 50 characters, e.g. Epic.", nameof(issueType));
        }

        return type;
    }

    private const int MaxComponentsLength = 1000;

    /// <summary>
    /// Component names trimmed, de-duplicated ignoring case and joined with commas, the same
    /// form a task keeps its components in.
    /// </summary>
    private static string NormalizeComponents(IEnumerable<string>? components)
    {
        var joined = string.Join(",", (components ?? Enumerable.Empty<string>())
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));

        if (joined.Length > MaxComponentsLength)
        {
            throw new ArgumentException($"A parent's components cannot exceed {MaxComponentsLength} characters.", nameof(components));
        }

        return joined;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parent name cannot be empty.", nameof(name));
        }

        if (name.Length > 500)
        {
            throw new ArgumentException("Parent name cannot exceed 500 characters.", nameof(name));
        }
    }
}
