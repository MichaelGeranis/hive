namespace Hive.Application.Interfaces;

/// <summary>
/// Read-only client for the Jira Cloud REST API. Hive never writes to Jira.
/// </summary>
public interface IJiraApiClient
{
    /// <summary>
    /// Gets every sprint on a Jira board, across all pages of results.
    /// </summary>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<IReadOnlyList<JiraSprint>> GetBoardSprintsAsync(
        JiraConnection connection,
        int boardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every parent issue in a Jira space (project), done ones included: every epic, and
    /// every other issue that has sub-tasks, such as a story or a support ticket.
    /// </summary>
    /// <param name="connection">Where Jira lives and how to authenticate.</param>
    /// <param name="projectKey">The space key, e.g. MAR.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<IReadOnlyList<JiraIssue>> GetParentIssuesAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every component of a Jira space (project).
    /// </summary>
    /// <param name="connection">Where Jira lives and how to authenticate.</param>
    /// <param name="projectKey">The space key, e.g. MAR.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<IReadOnlyList<JiraComponent>> GetComponentsAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every work item in a Jira space that is in at least one sprint, done ones
    /// included: stories, tasks, bugs, sub-tasks and the rest, but not epics. Story points
    /// and sprints are read from the site's own custom fields, found by name.
    /// </summary>
    /// <exception cref="JiraApiException">Jira rejected the request, could not be reached, or has no Sprint field.</exception>
    Task<IReadOnlyList<JiraWorkItem>> GetSprintWorkItemsAsync(
        JiraConnection connection,
        string projectKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Where Jira lives and how to authenticate against it.
/// </summary>
/// <param name="SiteUrl">The site root, e.g. https://yourteam.atlassian.net.</param>
/// <param name="Email">The Atlassian account email the API token belongs to.</param>
/// <param name="ApiToken">An Atlassian API token.</param>
public record JiraConnection(string SiteUrl, string Email, string ApiToken);

/// <summary>
/// A sprint as Jira reports it. Future sprints usually have no dates yet.
/// </summary>
public record JiraSprint(
    long Id,
    string Name,
    string State,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate
);

/// <summary>
/// An issue as Jira reports it, with the fields Hive reads.
/// </summary>
/// <param name="Key">The issue key, e.g. MAR-128.</param>
/// <param name="Summary">The issue title.</param>
/// <param name="IssueType">The issue type name, e.g. Epic, Story or Sub-task.</param>
/// <param name="Status">The workflow status name, e.g. Done.</param>
/// <param name="ParentKey">The key of the issue's parent, if it has one.</param>
/// <param name="Components">The names of the issue's components, which are Hive's products.</param>
public record JiraIssue(
    string Key,
    string Summary,
    string IssueType,
    string? Status,
    string? ParentKey = null,
    IReadOnlyList<string>? Components = null
);

/// <summary>
/// A component of a Jira space. Hive keeps one product per component.
/// </summary>
/// <param name="Id">Jira's id for the component, e.g. 10042. It survives a rename.</param>
public record JiraComponent(string Id, string Name, string? Description);

/// <summary>
/// A work item as Jira reports it, with every field a Hive task is built from.
/// </summary>
/// <param name="Description">The description as plain text; Jira stores it as rich text.</param>
/// <param name="AssigneeEmail">Null when the assignee hides their email.</param>
/// <param name="StoryPoints">As Jira holds them, which may be fractional.</param>
/// <param name="Sprints">Every sprint the item has been in, oldest first.</param>
public record JiraWorkItem(
    string Key,
    string Summary,
    string Description,
    string IssueType,
    string? Status,
    string? Priority,
    string? AssigneeEmail,
    string? AssigneeName,
    string? ParentKey,
    string? ProjectName,
    DateTime? DueDate,
    IReadOnlyList<string> Labels,
    IReadOnlyList<string> Components,
    double? StoryPoints,
    IReadOnlyList<JiraSprint> Sprints
);

/// <summary>
/// Jira rejected a request or could not be reached. The message is safe to show the manager.
/// </summary>
public class JiraApiException : Exception
{
    public JiraApiException(string message) : base(message)
    {
    }

    public JiraApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
