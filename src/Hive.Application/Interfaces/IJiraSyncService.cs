using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Pulls data from Jira on demand. One-way: Hive reads from Jira and never writes back.
/// </summary>
public interface IJiraSyncService
{
    /// <summary>
    /// Copies start and end dates from the configured Jira board onto the Hive sprints with
    /// the same name. Sprints are never created or deleted.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">Jira sync is not fully configured.</exception>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<JiraSprintSyncResultDto> SyncSprintDatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes Hive's products match the components of the configured Jira space. Creates a
    /// product for each new component, updates the name and description of the rest, and
    /// removes products whose component is gone. Products made by hand are never removed; one
    /// with the same name as a component is linked to it.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">Jira sync is not fully configured.</exception>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<JiraProductSyncResultDto> SyncProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes Hive's parents match the parent issues of the configured Jira space, done ones
    /// included: every epic, and every other issue with sub-tasks. Creates a parent for each
    /// new issue and updates the name, issue type, components and time spent of the rest.
    /// Parents from Jira whose issue is gone are removed.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">Jira sync is not fully configured.</exception>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<JiraParentSyncResultDto> SyncParentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes Hive's tasks match the work items in the team's sprints in the configured Jira
    /// space, done ones included, after syncing parents so every task links to its parent.
    /// Tasks from Jira that are no longer in those sprints are removed; tasks made in Hive
    /// by hand are never touched.
    /// </summary>
    /// <exception cref="Hive.Core.Exceptions.ValidationException">Jira sync is not fully configured.</exception>
    /// <exception cref="JiraApiException">Jira rejected the request or could not be reached.</exception>
    Task<JiraTaskSyncResultDto> SyncTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs every sync. A part that fails is reported in the result rather than thrown.
    /// </summary>
    Task<JiraSyncAllResultDto> SyncAllAsync(CancellationToken cancellationToken = default);
}
