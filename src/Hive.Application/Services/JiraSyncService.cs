using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using static Hive.Application.Services.JiraTaskRules;

namespace Hive.Application.Services;

/// <summary>
/// Pulls data from Jira on demand and applies it to Hive: sprint dates onto sprints,
/// components onto products, parent issues (epics, and anything else with sub-tasks) onto
/// parents, and sprint work items onto tasks.
/// </summary>
public class JiraSyncService : IJiraSyncService
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ISprintService _sprintService;
    private readonly IParentRepository _parentRepository;
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IActivityService _activityService;
    private readonly IJiraApiClient _jiraApiClient;
    private readonly ITeamTaskService _taskService;
    private readonly IParentService _parentService;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProjectProductRepository _projectProductRepository;

    public JiraSyncService(
        IAppSettingsRepository settingsRepository,
        ISprintRepository sprintRepository,
        ISprintService sprintService,
        IParentRepository parentRepository,
        ITeamTaskRepository taskRepository,
        IActivityService activityService,
        IJiraApiClient jiraApiClient,
        ITeamTaskService taskService,
        IParentService parentService,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        IProductRepository productRepository,
        IProjectProductRepository projectProductRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _projectProductRepository = projectProductRepository ?? throw new ArgumentNullException(nameof(projectProductRepository));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _sprintRepository = sprintRepository ?? throw new ArgumentNullException(nameof(sprintRepository));
        _sprintService = sprintService ?? throw new ArgumentNullException(nameof(sprintService));
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _jiraApiClient = jiraApiClient ?? throw new ArgumentNullException(nameof(jiraApiClient));
    }

    public async Task<JiraSyncAllResultDto> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var (sprintDates, sprintDatesError) = await RunPartAsync(SyncSprintDatesAsync, cancellationToken);
        var (products, productsError) = await RunPartAsync(SyncProductsAsync, cancellationToken);
        var (parents, parentsError) = await RunPartAsync(SyncParentsAsync, cancellationToken);
        // Parents were just synced, so the task sync does not repeat it
        var (tasks, tasksError) = await RunPartAsync(ct => SyncTasksWithoutParentsAsync(null, ct), cancellationToken);

        return new JiraSyncAllResultDto
        {
            SprintDates = sprintDates,
            SprintDatesError = sprintDatesError,
            Products = products,
            ProductsError = productsError,
            Parents = parents,
            ParentsError = parentsError,
            Tasks = tasks,
            TasksError = tasksError
        };
    }

    public async Task<JiraSprintSyncResultDto> SyncSprintDatesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetConfiguredSettingsAsync(s => s.GetMissingJiraSprintSyncSettings(), cancellationToken);
        var connection = ToConnection(settings);
        var jiraSprints = await _jiraApiClient.GetBoardSprintsAsync(connection, settings.JiraBoardId!.Value, cancellationToken);
        var hiveSprints = await _sprintRepository.GetAllAsync(cancellationToken);

        // Sprint names are the join between the two systems, as they are for the CSV import
        var jiraSprintsByName = jiraSprints
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var updated = new List<SprintDateChangeDto>();
        var missingDates = new List<string>();
        var notFound = new List<string>();
        var ambiguous = new List<string>();
        var errors = new List<string>();
        var unchanged = 0;

        foreach (var sprint in hiveSprints.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!jiraSprintsByName.TryGetValue(sprint.Name, out var matches))
            {
                notFound.Add(sprint.Name);
                continue;
            }

            if (matches.Count > 1)
            {
                ambiguous.Add(sprint.Name);
                continue;
            }

            var jiraSprint = matches[0];
            if (jiraSprint.StartDate is null || jiraSprint.EndDate is null)
            {
                missingDates.Add(sprint.Name);
                continue;
            }

            var startDate = ToCalendarDate(jiraSprint.StartDate.Value);
            var endDate = ToCalendarDate(jiraSprint.EndDate.Value);

            if (sprint.StartDate?.Date == startDate && sprint.EndDate?.Date == endDate)
            {
                unchanged++;
                continue;
            }

            var previousStart = sprint.StartDate;
            var previousEnd = sprint.EndDate;

            try
            {
                // Through the sprint service so capacity is recalculated and the change is
                // recorded in the activity feed, exactly as a manual edit would be
                await _sprintService.UpdateAsync(
                    sprint.Id,
                    new UpdateSprintDto { StartDate = startDate, EndDate = endDate },
                    cancellationToken);

                updated.Add(new SprintDateChangeDto
                {
                    SprintId = sprint.Id,
                    Name = sprint.Name,
                    PreviousStartDate = previousStart,
                    PreviousEndDate = previousEnd,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{sprint.Name}: {ex.Message}");
            }
        }

        // Sprints the team has planned in Jira that Hive does not have yet. Only future and
        // active ones: creating every closed sprint on the board would add empty past sprints
        // and drag down velocity, utilization and the next-sprint prediction.
        var hiveNames = hiveSprints.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var upcoming = jiraSprintsByName
            .Where(kvp => kvp.Value.Count == 1 &&
                          !hiveNames.Contains(kvp.Key) &&
                          IsUpcomingSprint(kvp.Value[0]) &&
                          IsTeamSprint(kvp.Key, settings.SprintTeamFilter))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => kvp.Value[0] with { Name = kvp.Key })
            .ToList();
        var created = new List<string>();
        foreach (var sprint in upcoming)
        {
            try
            {
                created.AddRange(await EnsureSprintsAsync([sprint], cancellationToken));
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{sprint.Name}: {ex.Message}");
            }
        }

        return new JiraSprintSyncResultDto
        {
            JiraSprintCount = jiraSprints.Count,
            Created = created,
            Updated = updated,
            UnchangedCount = unchanged,
            MissingDatesInJira = missingDates,
            NotFoundInJira = notFound,
            AmbiguousInJira = ambiguous,
            Errors = errors,
            SyncedAt = DateTime.UtcNow
        };
    }

    public async Task<JiraProductSyncResultDto> SyncProductsAsync(CancellationToken cancellationToken = default)
    {
        // Components belong to the space, so the product sync needs the same settings as parents
        var settings = await GetConfiguredSettingsAsync(s => s.GetMissingJiraParentSyncSettings(), cancellationToken);
        var components = await _jiraApiClient.GetComponentsAsync(ToConnection(settings), settings.JiraProjectKey!, cancellationToken);

        var products = (await _productRepository.GetAllAsync(cancellationToken)).ToList();

        var created = new List<ProductSyncChangeDto>();
        var updated = new List<ProductSyncChangeDto>();
        var errors = new List<string>();
        var unchanged = 0;

        foreach (var component in components)
        {
            var name = component.Name.Trim();

            // The component id is the identity. A product made by hand has none yet, so it is
            // recognised by name once and linked from then on.
            var product = products.FirstOrDefault(p => p.JiraComponentId == component.Id.Trim())
                ?? products.FirstOrDefault(p => !p.IsFromJira && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            // Product names are unique, so a component cannot take a name another product holds
            var nameHolder = products.FirstOrDefault(p => p != product && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (nameHolder is not null)
            {
                errors.Add($"{name}: another product already has this name.");
                continue;
            }

            try
            {
                if (product is null)
                {
                    product = Product.FromJira(component.Id, name, component.Description);
                    await _productRepository.AddAsync(product, cancellationToken);
                    products.Add(product);
                    created.Add(ToChange(product, previousName: null));
                    await LogAsync(ActivityType.Created, product, $"Product created from Jira component {product.Name}", cancellationToken);
                    continue;
                }

                if (product.MatchesJira(component.Id, name, component.Description))
                {
                    unchanged++;
                    continue;
                }

                var previousName = product.Name;
                product.SyncFromJira(component.Id, name, component.Description);
                await _productRepository.UpdateAsync(product, cancellationToken);
                updated.Add(ToChange(product, previousName == product.Name ? null : previousName));
                await LogAsync(ActivityType.Updated, product, $"Product updated from Jira component {product.Name}", cancellationToken);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errors.Add($"{name}: {ex.Message}");
            }
        }

        // Jira is the source of truth: a product from Jira whose component is gone goes too,
        // with its project links. Only after a non-empty fetch, so a failed or empty answer
        // never empties Hive. Products made by hand are the manager's and stay.
        var removed = new List<ProductSyncChangeDto>();
        if (components.Count > 0)
        {
            var fetchedIds = components.Select(c => c.Id.Trim()).ToHashSet();
            foreach (var stale in products.Where(p => p.IsFromJira && !fetchedIds.Contains(p.JiraComponentId!)).ToList())
            {
                await _projectProductRepository.DeleteByProductAsync(stale.Id, cancellationToken);
                await _productRepository.DeleteAsync(stale.Id, cancellationToken);
                removed.Add(ToChange(stale, previousName: null));
                await LogAsync(ActivityType.Deleted, stale, $"Product removed: Jira component {stale.Name} is gone", cancellationToken);
            }
        }

        return new JiraProductSyncResultDto
        {
            JiraComponentCount = components.Count,
            Created = created,
            Updated = updated,
            UnchangedCount = unchanged,
            Removed = removed,
            Errors = errors,
            SyncedAt = DateTime.UtcNow
        };
    }

    public async Task<JiraParentSyncResultDto> SyncParentsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetConfiguredSettingsAsync(s => s.GetMissingJiraParentSyncSettings(), cancellationToken);
        var issues = await _jiraApiClient.GetParentIssuesAsync(ToConnection(settings), settings.JiraProjectKey!, cancellationToken);

        var parents = (await _parentRepository.GetAllAsync(cancellationToken)).ToList();
        var taskIdsByJiraKey = await GetTaskIdsByJiraKeyAsync(cancellationToken);

        var created = new List<ParentSyncChangeDto>();
        var updated = new List<ParentSyncChangeDto>();
        var errors = new List<string>();
        var unchanged = 0;

        foreach (var issue in issues)
        {
            var name = issue.Summary.Trim();
            if (name.Length == 0)
            {
                errors.Add($"{issue.Key}: the issue has no summary.");
                continue;
            }


            // The key is the identity. A parent from a CSV import has no key yet, so it is
            // recognised by name once and linked from then on.
            var parent = parents.FirstOrDefault(p => string.Equals(p.JiraKey, issue.Key, StringComparison.OrdinalIgnoreCase))
                ?? parents.FirstOrDefault(p => p.JiraKey is null && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            // Parent names are unique, so an issue cannot take a name another parent holds
            var nameHolder = parents.FirstOrDefault(p => p != parent && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (nameHolder is not null)
            {
                errors.Add($"{issue.Key}: another parent is already called '{name}'{(nameHolder.JiraKey is null ? "" : $" ({nameHolder.JiraKey})")}.");
                continue;
            }

            try
            {
                if (parent is null)
                {
                    parent = Parent.FromJira(issue.Key, issue.IssueType, name, issue.Components);
                    LinkOwnTask(parent, issue.Key, taskIdsByJiraKey);
                    await _parentRepository.AddAsync(parent, cancellationToken);
                    parents.Add(parent);
                    created.Add(ToChange(parent, previousName: null));
                    await LogAsync(ActivityType.Created, parent, $"Parent created from Jira {parent.JiraIssueType} {parent.JiraKey}", cancellationToken);
                    continue;
                }

                var taskLinked = LinkOwnTask(parent, issue.Key, taskIdsByJiraKey);
                if (parent.MatchesJira(issue.Key, issue.IssueType, name, issue.Components))
                {
                    if (taskLinked)
                    {
                        await _parentRepository.UpdateAsync(parent, cancellationToken);
                    }

                    unchanged++;
                    continue;
                }

                var previousName = parent.Name;
                parent.SyncFromJira(issue.Key, issue.IssueType, name, issue.Components);
                await _parentRepository.UpdateAsync(parent, cancellationToken);
                updated.Add(ToChange(parent, previousName == parent.Name ? null : previousName));
                await LogAsync(ActivityType.Updated, parent, $"Parent updated from Jira {parent.JiraIssueType} {parent.JiraKey}", cancellationToken);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errors.Add($"{issue.Key}: {ex.Message}");
            }
        }

        // Jira is the source of truth: a parent from Jira whose issue is gone goes too. Only
        // after a complete, non-empty fetch, so a failed or empty answer never empties Hive.
        var removed = new List<ParentSyncChangeDto>();
        if (issues.Count > 0)
        {
            var fetchedKeys = issues.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var stale in parents.Where(p => p.JiraKey is not null && !fetchedKeys.Contains(p.JiraKey)).ToList())
            {
                await RemoveParentAsync(stale, cancellationToken);
                removed.Add(ToChange(stale, previousName: null));
            }
        }

        return new JiraParentSyncResultDto
        {
            JiraParentCount = issues.Count,
            Created = created,
            Updated = updated,
            UnchangedCount = unchanged,
            Removed = removed,
            Errors = errors,
            SyncedAt = DateTime.UtcNow
        };
    }

    public async Task<JiraTaskSyncResultDto> SyncTasksAsync(CancellationToken cancellationToken = default)
    {
        // Parents first, so every task finds the parent its Jira parent key names
        var parents = await SyncParentsAsync(cancellationToken);
        return await SyncTasksWithoutParentsAsync(parents, cancellationToken);
    }

    private async Task<JiraTaskSyncResultDto> SyncTasksWithoutParentsAsync(
        JiraParentSyncResultDto? parentResult,
        CancellationToken cancellationToken)
    {
        var settings = await GetConfiguredSettingsAsync(s => s.GetMissingJiraParentSyncSettings(), cancellationToken);
        var items = await _jiraApiClient.GetSprintWorkItemsAsync(ToConnection(settings), settings.JiraProjectKey!, cancellationToken);

        // Only the team's sprints count, exactly as for the CSV import: the sprint naming
        // pattern, and the team filter when one is set
        var inScope = items
            .Select(item => (Item: item, Sprints: item.Sprints.Where(s => IsTeamSprint(s.Name, settings.SprintTeamFilter)).ToList()))
            .Where(x => x.Sprints.Count > 0)
            .ToList();

        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var parents = await _parentRepository.GetAllAsync(cancellationToken);
        var parentIdsByKey = parents
            .Where(p => p.JiraKey is not null)
            .ToDictionary(p => p.JiraKey!, p => p.Id, StringComparer.OrdinalIgnoreCase);

        // One task per Jira issue. Older CSV imports sometimes created the same issue twice;
        // the oldest copy is kept and the others are removed with the stale tasks below.
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var tasksByKey = new Dictionary<string, TeamTask>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<(string Key, TeamTask Task)>();
        foreach (var task in tasks.OrderBy(t => t.CreatedAt))
        {
            if (task.GetJiraKey() is not { } key)
            {
                continue;
            }

            if (!tasksByKey.TryAdd(key, task))
            {
                duplicates.Add((key, task));
            }
        }

        var sprintsCreated = await EnsureSprintsAsync(inScope.SelectMany(x => x.Sprints), cancellationToken);

        var created = new List<TaskSyncChangeDto>();
        var updated = new List<TaskSyncChangeDto>();
        var errors = new List<string>();
        var unchanged = 0;

        foreach (var (item, sprints) in inScope)
        {
            try
            {
                var data = new JiraTaskData
                {
                    Title = string.IsNullOrWhiteSpace(item.Summary) ? "Untitled Task" : item.Summary,
                    Description = item.Description,
                    Type = MapIssueTypeToTaskType(item.IssueType),
                    Status = MapJiraStatusToTaskStatus(item.Status),
                    Priority = MapJiraPriorityToTaskPriority(item.Priority),
                    AssigneeId = FindAssigneeId(item.AssigneeEmail, item.AssigneeName, directReports),
                    ProjectId = FindProjectId(item.ProjectName, projects),
                    StoryPoints = ToStoryPoints(item.StoryPoints),
                    DueDate = item.DueDate,
                    Tags = $"jira:{item.Key}",
                    Labels = JoinValues(item.Labels),
                    Components = JoinValues(item.Components),
                    Sprint = JoinValues(sprints.Select(s => s.Name)),
                };

                Guid? parentId = item.ParentKey is not null && parentIdsByKey.TryGetValue(item.ParentKey, out var id) ? id : null;

                if (tasksByKey.TryGetValue(item.Key, out var existing))
                {
                    var before = Fingerprint(existing);
                    ApplyToExisting(existing, data, parentId);
                    if (Fingerprint(existing) == before)
                    {
                        unchanged++;
                        continue;
                    }

                    await _taskRepository.UpdateAsync(existing, cancellationToken);
                    updated.Add(new TaskSyncChangeDto { TaskId = existing.Id, JiraKey = item.Key, Title = existing.Title });
                }
                else
                {
                    var task = await _taskRepository.AddAsync(CreateTask(data, parentId), cancellationToken);
                    tasksByKey[item.Key] = task;
                    created.Add(new TaskSyncChangeDto { TaskId = task.Id, JiraKey = item.Key, Title = task.Title });
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                errors.Add($"{item.Key}: {ex.Message}");
            }
        }

        // A parent issue that is also a task in a sprint, e.g. a story with sub-tasks
        foreach (var parent in parents.Where(p => p.JiraKey is not null && !p.TeamTaskId.HasValue))
        {
            if (tasksByKey.TryGetValue(parent.JiraKey!, out var ownTask))
            {
                parent.LinkToTask(ownTask.Id);
                await _parentRepository.UpdateAsync(parent, cancellationToken);
            }
        }

        // Jira is the source of truth: a task from Jira that is no longer in the team's
        // sprints goes. Only after a complete, non-empty fetch; tasks made by hand stay.
        var removed = new List<TaskSyncChangeDto>();
        if (inScope.Count > 0)
        {
            var inScopeKeys = inScope.Select(x => x.Item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var stale = tasksByKey.Where(t => !inScopeKeys.Contains(t.Key)).Select(t => (t.Key, Task: t.Value));
            foreach (var (key, task) in stale.Concat(duplicates).ToList())
            {
                await _taskService.DeleteAsync(task.Id, cancellationToken);
                removed.Add(new TaskSyncChangeDto { TaskId = task.Id, JiraKey = key, Title = task.Title });
            }
        }

        return new JiraTaskSyncResultDto
        {
            JiraTaskCount = inScope.Count,
            Created = created,
            Updated = updated,
            UnchangedCount = unchanged,
            Removed = removed,
            OutOfScopeCount = items.Count - inScope.Count,
            SprintsCreated = sprintsCreated,
            Errors = errors,
            Parents = parentResult,
            SyncedAt = DateTime.UtcNow
        };
    }

    private static bool IsUpcomingSprint(JiraSprint sprint) =>
        string.Equals(sprint.State, "future", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sprint.State, "active", StringComparison.OrdinalIgnoreCase);

    private static bool IsTeamSprint(string sprintName, string? teamFilter) =>
        IsValidSprintPattern(sprintName) &&
        (string.IsNullOrWhiteSpace(teamFilter) ||
         string.Equals(ExtractTeamNameFromSprint(sprintName), teamFilter, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Creates the sprints the tasks are in that Hive does not have yet, with Jira's dates.
    /// Dates of existing sprints are left to the sprint date sync.
    /// </summary>
    private async Task<List<string>> EnsureSprintsAsync(IEnumerable<JiraSprint> sprints, CancellationToken cancellationToken)
    {
        var createdNames = new List<string>();
        foreach (var sprint in sprints.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
        {
            var existed = await _sprintRepository.ExistsAsync(sprint.Name, cancellationToken);
            var hiveSprint = await _sprintService.GetOrCreateAsync(sprint.Name, cancellationToken);
            if (existed)
            {
                continue;
            }

            createdNames.Add(sprint.Name);
            if (sprint.StartDate is { } start && sprint.EndDate is { } end)
            {
                await _sprintService.UpdateAsync(
                    hiveSprint.Id,
                    new UpdateSprintDto { StartDate = ToCalendarDate(start), EndDate = ToCalendarDate(end) },
                    cancellationToken);
            }
        }

        return createdNames;
    }

    /// <summary>
    /// Removes a parent whose Jira issue is gone. Any task still pointing at it is unlinked
    /// first, so no task is left pointing at a parent that no longer exists.
    /// </summary>
    private async Task RemoveParentAsync(Parent parent, CancellationToken cancellationToken)
    {
        foreach (var task in await _taskRepository.GetByParentIdAsync(parent.Id, cancellationToken))
        {
            task.AssignToParent(null);
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }

        await _parentService.DeleteAsync(parent.Id, cancellationToken);
    }

    private async Task<AppSettings> GetConfiguredSettingsAsync(
        Func<AppSettings, IReadOnlyList<string>> missingFor,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var missing = settings is null
            ? new[] { "Jira base URL", "Jira email", "Jira API token" }
            : missingFor(settings);

        if (settings is null || missing.Count > 0)
        {
            throw new ValidationException(
                $"Jira sync is not configured. Add the {string.Join(", ", missing)} in Settings.");
        }

        return settings;
    }

    private static JiraConnection ToConnection(AppSettings settings) =>
        new(settings.GetJiraSiteUrl()!, settings.JiraEmail!, settings.JiraApiToken!);

    private static async Task<(T? Result, string? Error)> RunPartAsync<T>(
        Func<CancellationToken, Task<T>> sync,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            return (await sync(cancellationToken), null);
        }
        catch (DomainException ex)
        {
            return (null, ex.Message);
        }
        catch (JiraApiException ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Imported tasks carry their Jira key as a <c>jira:KEY</c> tag. A parent issue that was
    /// also imported as a task, such as an epic, is linked to that task through it.
    /// </summary>
    private async Task<Dictionary<string, Guid>> GetTaskIdsByJiraKeyAsync(CancellationToken cancellationToken)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var byKey = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in tasks)
        {
            foreach (var tag in task.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (tag.StartsWith("jira:", StringComparison.OrdinalIgnoreCase))
                {
                    byKey.TryAdd(tag["jira:".Length..], task.Id);
                }
            }
        }

        return byKey;
    }

    private static bool LinkOwnTask(Parent parent, string jiraKey, IReadOnlyDictionary<string, Guid> taskIdsByJiraKey)
    {
        if (parent.TeamTaskId.HasValue || !taskIdsByJiraKey.TryGetValue(jiraKey, out var taskId))
        {
            return false;
        }

        parent.LinkToTask(taskId);
        return true;
    }

    private Task LogAsync(ActivityType type, Parent parent, string description, CancellationToken cancellationToken) =>
        _activityService.LogActivityAsync(type, EntityType.Parent, parent.Id, parent.Name, description, cancellationToken);

    private Task LogAsync(ActivityType type, Product product, string description, CancellationToken cancellationToken) =>
        _activityService.LogActivityAsync(type, EntityType.Product, product.Id, product.Name, description, cancellationToken);

    private static ProductSyncChangeDto ToChange(Product product, string? previousName) => new()
    {
        ProductId = product.Id,
        JiraComponentId = product.JiraComponentId!,
        Name = product.Name,
        PreviousName = previousName
    };

    private static ParentSyncChangeDto ToChange(Parent parent, string? previousName) => new()
    {
        ParentId = parent.Id,
        JiraKey = parent.JiraKey!,
        Name = parent.Name,
        PreviousName = previousName
    };

    /// <summary>
    /// Jira records a sprint boundary as an instant; Hive stores sprint dates as calendar
    /// days, the same as a date picked by hand. The day is taken in the local time zone of
    /// the machine Hive runs on, which is the manager's own.
    /// </summary>
    private static DateTime ToCalendarDate(DateTimeOffset instant) =>
        DateTime.SpecifyKind(instant.ToLocalTime().Date, DateTimeKind.Unspecified);
}
