using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using TaskStatus = Hive.Core.Entities.TaskStatus;

namespace Hive.Application.Services;

/// <summary>
/// Service for importing tasks from Jira CSV exports.
/// </summary>
public class JiraImportService : IJiraImportService
{
    private readonly ITeamTaskRepository _taskRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintService _sprintService;
    private readonly IParentService _parentService;
    private readonly IAppSettingsService _appSettingsService;

    // Sprint name pattern: TeamName_QuarterQYear_SSprintNumber (e.g., LP_1Q25_S4)
    private static readonly Regex SprintPatternRegex = new(@"^(\w+)_(\d)Q(\d{2})_S(\d+)$", RegexOptions.Compiled);

    // Common Jira CSV column names
    private static readonly string[] IssueKeyColumns = { "Issue key", "Key", "Issue Key", "IssueKey" };
    private static readonly string[] SummaryColumns = { "Summary", "Title", "Subject" };
    private static readonly string[] DescriptionColumns = { "Description" };
    private static readonly string[] IssueTypeColumns = { "Issue Type", "Type", "IssueType" };
    private static readonly string[] StatusColumns = { "Status" };
    private static readonly string[] PriorityColumns = { "Priority" };
    private static readonly string[] AssigneeColumns = { "Assignee" };
    private static readonly string[] StoryPointsColumns = { "Story Points", "StoryPoints", "Story points", "Custom field (Story Points)" };
    private static readonly string[] ProjectColumns = { "Project", "Project name", "ProjectName" };
    private static readonly string[] DueDateColumns = { "Due date", "DueDate", "Due Date" };
    private static readonly string[] TimeSpentColumns = { "Σ Time Spent" };
    private static readonly string[] SprintColumns = { "Sprint" };
    private static readonly string[] LabelsColumns = { "Labels", "Label" };
    private static readonly string[] ParentColumns = { "Parent Summary", "Parent summary" };
    private static readonly string[] ParentKeyColumns = { "Parent key", "Parent Key", "ParentKey" };

    public JiraImportService(
        ITeamTaskRepository taskRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository,
        ISprintService sprintService,
        IParentService parentService,
        IAppSettingsService appSettingsService)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _sprintService = sprintService ?? throw new ArgumentNullException(nameof(sprintService));
        _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
    }

    public Task<JiraImportPreviewDto> PreviewImportAsync(string csvContent, CancellationToken cancellationToken = default)
    {
        var lines = ParseCsvLines(csvContent);
        if (lines.Count == 0)
        {
            return Task.FromResult(new JiraImportPreviewDto
            {
                TotalRows = 0,
                ValidRows = 0,
                InvalidRows = 0,
                DetectedColumns = new List<string>(),
                MappingWarnings = new List<string> { "CSV file is empty" }
            });
        }

        var headers = ParseCsvRow(lines[0]);
        var detectedColumns = headers.ToList();
        var warnings = ValidateHeaders(headers);

        var sampleRows = new List<JiraImportPreviewRowDto>();
        var validCount = 0;
        var invalidCount = 0;

        // Validate ALL rows, but only add first 10 to sample
        for (int i = 1; i < lines.Count; i++)
        {
            var values = ParseCsvRow(lines[i]);
            var rowData = MapRowToDictionary(headers, values);
            var previewRow = CreatePreviewRow(i + 1, rowData);

            // Only add first 10 rows to sample display
            if (i <= 10)
            {
                sampleRows.Add(previewRow);
            }

            if (previewRow.IsValid)
                validCount++;
            else
                invalidCount++;
        }

        return Task.FromResult(new JiraImportPreviewDto
        {
            TotalRows = lines.Count - 1, // Exclude header
            ValidRows = validCount,
            InvalidRows = invalidCount,
            DetectedColumns = detectedColumns,
            MappingWarnings = warnings,
            SampleRows = sampleRows
        });
    }

    public async Task<JiraImportResultDto> ImportAsync(JiraImportRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = new JiraImportResultDto();
        var errors = new List<string>();
        var warnings = new List<string>();
        var importedTasks = new List<JiraImportedTaskDto>();

        var lines = ParseCsvLines(request.CsvContent);
        if (lines.Count == 0)
        {
            errors.Add("CSV file is empty");
            return new JiraImportResultDto
            {
                TotalRows = 0,
                ErrorCount = 1,
                Errors = errors
            };
        }

        var headers = ParseCsvRow(lines[0]);
        var headerValidation = ValidateHeaders(headers);
        warnings.AddRange(headerValidation);

        // Load all existing tasks, direct reports, projects, and app settings once
        var existingTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var appSettings = await _appSettingsService.GetAsync(cancellationToken);

        var totalRows = lines.Count - 1;
        var successCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        // Track created sprints and parents to avoid duplicate creation attempts
        var createdSprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var createdParents = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        // First pass: collect all Parent keys to identify which Issue keys are parents
        var parentIssueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Count; i++)
        {
            var values = ParseCsvRow(lines[i]);
            var rowData = MapRowToDictionary(headers, values);
            var parentKey = GetValue(rowData, ParentKeyColumns);
            if (!string.IsNullOrWhiteSpace(parentKey))
            {
                parentIssueKeys.Add(parentKey);
            }
        }

        for (int i = 1; i < lines.Count; i++)
        {
            try
            {
                var values = ParseCsvRow(lines[i]);
                var rowData = MapRowToDictionary(headers, values);

                var issueKey = GetValue(rowData, IssueKeyColumns);
                var summary = GetValue(rowData, SummaryColumns);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(issueKey) && string.IsNullOrWhiteSpace(summary))
                {
                    errors.Add($"Row {i + 1}: Missing both Issue Key and Summary");
                    errorCount++;
                    continue;
                }

                // Check for existing task
                var existingTask = FindExistingTask(existingTasks, issueKey, summary, request.MatchField);

                if (existingTask != null && !request.UpdateExisting)
                {
                    warnings.Add($"Row {i + 1}: Task '{issueKey}' already exists, skipping");
                    skippedCount++;
                    continue;
                }

                // Filter and validate sprints before mapping
                // Only process sprints that match the pattern: TeamName_QuarterQYear_SSprintNumber
                // If SprintTeamFilter is set in settings, additionally filter by team name
                var sprintTeamFilter = appSettings?.SprintTeamFilter;
                var sprintValue = GetValue(rowData, SprintColumns);
                if (!string.IsNullOrWhiteSpace(sprintValue))
                {
                    var sprintNames = sprintValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var validSprints = new List<string>();

                    foreach (var sprintName in sprintNames)
                    {
                        if (!IsValidSprintPattern(sprintName))
                        {
                            warnings.Add($"Row {i + 1}: Sprint '{sprintName}' doesn't match required pattern (TeamName_QuarterQYear_SSprintNumber), ignoring");
                            continue;
                        }

                        // If team filter is set, check if sprint's team name matches
                        if (!string.IsNullOrWhiteSpace(sprintTeamFilter))
                        {
                            var sprintTeamName = ExtractTeamNameFromSprint(sprintName);
                            if (!string.Equals(sprintTeamName, sprintTeamFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                warnings.Add($"Row {i + 1}: Sprint '{sprintName}' team '{sprintTeamName}' doesn't match configured team filter '{sprintTeamFilter}', ignoring");
                                continue;
                            }
                        }

                        validSprints.Add(sprintName);
                    }

                    // If task had sprints but none were valid, skip this task
                    if (sprintNames.Length > 0 && validSprints.Count == 0)
                    {
                        warnings.Add($"Row {i + 1}: Task '{summary}' has no valid sprints matching the required pattern/team filter, skipping");
                        skippedCount++;
                        continue;
                    }

                    // Update rowData to only contain valid sprints
                    if (validSprints.Count > 0)
                    {
                        // Update all possible sprint column variations
                        foreach (var sprintCol in SprintColumns)
                        {
                            if (rowData.ContainsKey(sprintCol))
                            {
                                rowData[sprintCol] = string.Join(",", validSprints);
                            }
                        }
                    }
                }

                // Map fields
                var taskData = MapJiraRowToTask(rowData, directReports, projects, appSettings, out var projectName);

                // Add warning if project was specified but not found
                if (!string.IsNullOrWhiteSpace(projectName) && !taskData.ProjectId.HasValue)
                {
                    warnings.Add($"Row {i + 1}: Project '{projectName}' not found, leaving ProjectId null");
                }

                // Auto-create valid sprints
                if (!string.IsNullOrWhiteSpace(taskData.Sprint))
                {
                    var sprintNames = taskData.Sprint.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var sprintName in sprintNames)
                    {
                        if (!createdSprints.Contains(sprintName))
                        {
                            try
                            {
                                await _sprintService.GetOrCreateAsync(sprintName, cancellationToken);
                                createdSprints.Add(sprintName);
                            }
                            catch (Exception ex)
                            {
                                warnings.Add($"Row {i + 1}: Failed to create sprint '{sprintName}': {ex.Message}");
                            }
                        }
                    }
                }

                // Auto-create parent and get ParentId
                Guid? parentId = null;
                var parentName = GetValue(rowData, ParentColumns);
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    if (createdParents.TryGetValue(parentName, out var cachedParentId))
                    {
                        parentId = cachedParentId;
                    }
                    else
                    {
                        try
                        {
                            var parent = await _parentService.GetOrCreateAsync(parentName, null, null, cancellationToken);
                            parentId = parent.Id;
                            createdParents[parentName] = parent.Id;
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"Row {i + 1}: Failed to create parent '{parentName}': {ex.Message}");
                        }
                    }
                }

                Guid taskId;
                if (existingTask != null)
                {
                    // Update existing task
                    existingTask.Update(
                        taskData.Title,
                        taskData.Description,
                        taskData.Type,
                        taskData.Priority,
                        taskData.DueDate,
                        taskData.EstimatedHours,
                        taskData.StoryPoints,
                        taskData.Tags,
                        taskData.Labels,
                        taskData.Sprint,
                        taskData.TimeSpentMinutes,
                        parentId);

                    if (taskData.AssigneeId != existingTask.AssigneeId)
                    {
                        existingTask.AssignTo(taskData.AssigneeId);
                    }

                    if (taskData.ProjectId != existingTask.ProjectId)
                    {
                        existingTask.AssignToProject(taskData.ProjectId);
                    }

                    // Update status
                    UpdateTaskStatus(existingTask, taskData.Status);

                    await _taskRepository.UpdateAsync(existingTask, cancellationToken);
                    taskId = existingTask.Id;

                    importedTasks.Add(new JiraImportedTaskDto
                    {
                        TaskId = existingTask.Id,
                        IssueKey = issueKey ?? "N/A",
                        Summary = summary ?? "N/A",
                        IsNew = false,
                        IsUpdated = true
                    });
                }
                else
                {
                    // Create new task
                    var newTask = new TeamTask(
                        taskData.Title,
                        taskData.Description,
                        taskData.Type,
                        taskData.Priority,
                        taskData.AssigneeId,
                        taskData.ProjectId,
                        taskData.DueDate,
                        taskData.EstimatedHours,
                        taskData.StoryPoints,
                        taskData.Tags,
                        taskData.Labels,
                        taskData.Sprint,
                        taskData.TimeSpentMinutes,
                        parentId);

                    // Set status
                    UpdateTaskStatus(newTask, taskData.Status);

                    var created = await _taskRepository.AddAsync(newTask, cancellationToken);
                    taskId = created.Id;

                    importedTasks.Add(new JiraImportedTaskDto
                    {
                        TaskId = created.Id,
                        IssueKey = issueKey ?? "N/A",
                        Summary = summary ?? "N/A",
                        IsNew = true,
                        IsUpdated = false
                    });
                }

                // If task is an Epic OR is referenced as a parent by other tasks,
                // create/update a Parent entity with the same name, time spent, and link to task
                var isParentTask = !string.IsNullOrWhiteSpace(issueKey) && parentIssueKeys.Contains(issueKey);
                if ((taskData.Type == TaskType.Epic || isParentTask) && !string.IsNullOrWhiteSpace(taskData.Title))
                {
                    try
                    {
                        await _parentService.GetOrCreateAsync(taskData.Title, taskData.TimeSpentMinutes, taskId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Row {i + 1}: Failed to create parent for '{taskData.Title}': {ex.Message}");
                    }
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {i + 1}: {ex.Message}");
                errorCount++;
            }
        }

        return new JiraImportResultDto
        {
            TotalRows = totalRows,
            SuccessCount = successCount,
            SkippedCount = skippedCount,
            ErrorCount = errorCount,
            Errors = errors,
            Warnings = warnings,
            ImportedTasks = importedTasks
        };
    }

    private static List<string> ParseCsvLines(string csvContent)
    {
        return csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static List<string> ParseCsvRow(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        var insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (c == ',' && !insideQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString().Trim());
        return values;
    }

    private static Dictionary<string, string> MapRowToDictionary(List<string> headers, List<string> values)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
        {
            var header = headers[i];
            var value = values[i];

            // Handle duplicate columns (like Labels, Sprint) by appending values
            if (result.TryGetValue(header, out var existing))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result[header] = string.IsNullOrWhiteSpace(existing)
                        ? value
                        : $"{existing},{value}";
                }
            }
            else
            {
                result[header] = value;
            }
        }
        return result;
    }

    private static List<string> ValidateHeaders(List<string> headers)
    {
        var warnings = new List<string>();

        if (!headers.Any(h => IssueKeyColumns.Contains(h, StringComparer.OrdinalIgnoreCase)))
        {
            warnings.Add("No 'Issue key' column detected. Tasks will be matched by Title only.");
        }

        if (!headers.Any(h => SummaryColumns.Contains(h, StringComparer.OrdinalIgnoreCase)))
        {
            warnings.Add("No 'Summary' column detected. This is required for task creation.");
        }

        return warnings;
    }

    private static JiraImportPreviewRowDto CreatePreviewRow(int rowNumber, Dictionary<string, string> rowData)
    {
        var issueKey = GetValue(rowData, IssueKeyColumns) ?? string.Empty;
        var summary = GetValue(rowData, SummaryColumns) ?? string.Empty;
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(issueKey) && string.IsNullOrWhiteSpace(summary))
        {
            validationErrors.Add("Missing both Issue Key and Summary");
        }
        else if (string.IsNullOrWhiteSpace(summary))
        {
            validationErrors.Add("Missing Summary");
        }

        return new JiraImportPreviewRowDto
        {
            RowNumber = rowNumber,
            IssueKey = issueKey,
            Summary = summary,
            IssueType = GetValue(rowData, IssueTypeColumns) ?? string.Empty,
            Status = GetValue(rowData, StatusColumns) ?? string.Empty,
            Priority = GetValue(rowData, PriorityColumns) ?? string.Empty,
            Assignee = GetValue(rowData, AssigneeColumns) ?? string.Empty,
            StoryPoints = GetValue(rowData, StoryPointsColumns),
            IsValid = validationErrors.Count == 0,
            ValidationErrors = validationErrors
        };
    }

    private static string? GetValue(Dictionary<string, string> data, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    private static bool IsValidSprintPattern(string sprintName)
    {
        if (string.IsNullOrWhiteSpace(sprintName))
            return false;

        return SprintPatternRegex.IsMatch(sprintName);
    }

    private static string? ExtractTeamNameFromSprint(string sprintName)
    {
        if (string.IsNullOrWhiteSpace(sprintName))
            return null;

        var match = SprintPatternRegex.Match(sprintName);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static TeamTask? FindExistingTask(IReadOnlyList<TeamTask> existingTasks, string? issueKey, string? summary, string matchField)
    {
        if (matchField == "IssueKey" && !string.IsNullOrWhiteSpace(issueKey))
        {
            // Match by Issue Key in Tags
            return existingTasks.FirstOrDefault(t =>
                t.Tags.Contains($"jira:{issueKey}", StringComparison.OrdinalIgnoreCase));
        }
        else if (!string.IsNullOrWhiteSpace(summary))
        {
            // Match by exact title
            return existingTasks.FirstOrDefault(t =>
                t.Title.Equals(summary, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static TaskData MapJiraRowToTask(
        Dictionary<string, string> rowData,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Project> projects,
        AppSettingsDto appSettings,
        out string? projectNameOut)
    {
        var issueKey = GetValue(rowData, IssueKeyColumns);
        var summary = GetValue(rowData, SummaryColumns) ?? "Untitled Task";
        var description = GetValue(rowData, DescriptionColumns) ?? string.Empty;
        var issueType = GetValue(rowData, IssueTypeColumns);
        var status = GetValue(rowData, StatusColumns);
        var priority = GetValue(rowData, PriorityColumns);
        var assigneeName = GetValue(rowData, AssigneeColumns);
        var storyPointsStr = GetValue(rowData, StoryPointsColumns);
        var projectName = GetValue(rowData, ProjectColumns);
        projectNameOut = projectName; // Output the project name for warning if not found
        var dueDateStr = GetValue(rowData, DueDateColumns);
        var timeSpentStr = GetValue(rowData, TimeSpentColumns);

        // Collect all Labels and Sprint values (Jira exports multiple columns with same name)
        var labels = GetAllValues(rowData, LabelsColumns);
        var sprints = GetAllValues(rowData, SprintColumns);

        // Map fields
        var taskType = MapIssueTypeToTaskType(issueType);
        var taskStatus = MapJiraStatusToTaskStatus(status);
        var taskPriority = MapJiraPriorityToTaskPriority(priority);
        var assigneeId = FindAssigneeId(assigneeName, directReports);
        var projectId = FindProjectId(projectName, projects);
        var storyPoints = ParseStoryPoints(storyPointsStr);
        var dueDate = ParseDueDate(dueDateStr);
        var timeSpentMinutes = ParseTimeSpent(timeSpentStr);

        // Calculate estimated hours from story points using the mapping
        var estimatedHours = CalculateEstimatedHoursFromStoryPoints(storyPoints, appSettings);

        // Build tags with Jira Issue Key
        var tags = string.IsNullOrWhiteSpace(issueKey)
            ? "imported-from-jira"
            : $"jira:{issueKey},imported-from-jira";

        return new TaskData
        {
            Title = summary,
            Description = description,
            Type = taskType,
            Status = taskStatus,
            Priority = taskPriority,
            AssigneeId = assigneeId,
            ProjectId = projectId,
            StoryPoints = storyPoints,
            EstimatedHours = estimatedHours,
            DueDate = dueDate,
            Tags = tags,
            Labels = labels,
            Sprint = sprints,
            TimeSpentMinutes = timeSpentMinutes
        };
    }

    private static TaskType MapIssueTypeToTaskType(string? issueType)
    {
        if (string.IsNullOrWhiteSpace(issueType))
            return TaskType.Task;

        return issueType.ToLowerInvariant() switch
        {
            "epic" => TaskType.Epic,
            "story" => TaskType.Story,
            "sub-task" or "subtask" => TaskType.SubTask,
            "bug" => TaskType.Bug,
            "spike" => TaskType.Spike,
            "support" => TaskType.Support,
            "task" => TaskType.Task,
            _ => TaskType.Task
        };
    }

    private static TaskStatus MapJiraStatusToTaskStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return TaskStatus.Backlog;

        var lower = status.ToLowerInvariant().Replace(" ", "");

        return lower switch
        {
            // Backlog
            "backlog" => TaskStatus.Backlog,

            // To Do
            "todo" => TaskStatus.Todo,
            "to do" => TaskStatus.Todo,
            "toDo" => TaskStatus.Todo,
            "selected for development" => TaskStatus.Todo,
            "selectedfordevelopment" => TaskStatus.Todo,
            "open" => TaskStatus.Todo,

            // Blocked
            "blocked" => TaskStatus.Blocked,
            "blocker" => TaskStatus.Blocked,
            "impediment" => TaskStatus.Blocked,

            // In Progress
            "inprogress" => TaskStatus.InProgress,
            "in progress" => TaskStatus.InProgress,
            "inProgress" => TaskStatus.InProgress,
            "development" => TaskStatus.InProgress,
            "indevelopment" => TaskStatus.InProgress,

            // In Review
            "inreview" => TaskStatus.InReview,
            "in review" => TaskStatus.InReview,
            "inReview" => TaskStatus.InReview,
            "review" => TaskStatus.InReview,
            "code review" => TaskStatus.InReview,
            "codeReview" => TaskStatus.InReview,
            "peer review" => TaskStatus.InReview,
            "peerreview" => TaskStatus.InReview,

            // In Test
            "intest" => TaskStatus.InTest,
            "in test" => TaskStatus.InTest,
            "inTest" => TaskStatus.InTest,
            "testing" => TaskStatus.InTest,
            "qa" => TaskStatus.InTest,
            "qualityassurance" => TaskStatus.InTest,

            // PO Acceptance
            "poacceptance" => TaskStatus.POAcceptance,
            "po acceptance" => TaskStatus.POAcceptance,
            "poAcceptance" => TaskStatus.POAcceptance,
            "productowneracceptance" => TaskStatus.POAcceptance,
            "acceptance" => TaskStatus.POAcceptance,
            "uat" => TaskStatus.POAcceptance,
            "useracceptancetesting" => TaskStatus.POAcceptance,

            // Ready To Release
            "readytorelease" => TaskStatus.ReadyToRelease,
            "ready to release" => TaskStatus.ReadyToRelease,
            "readyToRelease" => TaskStatus.ReadyToRelease,
            "readyfordeploy" => TaskStatus.ReadyToRelease,
            "ready for deploy" => TaskStatus.ReadyToRelease,
            "deployready" => TaskStatus.ReadyToRelease,

            // Done
            "done" => TaskStatus.Done,
            "closed" => TaskStatus.Done,
            "resolved" => TaskStatus.Done,
            "complete" => TaskStatus.Done,
            "completed" => TaskStatus.Done,
            "released" => TaskStatus.Done,
            "deployed" => TaskStatus.Done,

            // Cancelled
            "cancelled" => TaskStatus.Cancelled,
            "canceled" => TaskStatus.Cancelled,
            "rejected" => TaskStatus.Cancelled,
            "wontdo" => TaskStatus.Cancelled,
            "won'tdo" => TaskStatus.Cancelled,

            // Default to Backlog for anything not recognized
            _ => TaskStatus.Backlog
        };
    }

    private static TaskPriority MapJiraPriorityToTaskPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return TaskPriority.Medium;

        return priority.ToLowerInvariant() switch
        {
            "lowest" => TaskPriority.Low,
            "low" => TaskPriority.Low,
            "medium" => TaskPriority.Medium,
            "normal" => TaskPriority.Medium,
            "high" => TaskPriority.High,
            "highest" => TaskPriority.Critical,
            "critical" => TaskPriority.Critical,
            "blocker" => TaskPriority.Critical,
            _ => TaskPriority.Medium
        };
    }

    private static Guid? FindAssigneeId(string? assigneeName, IReadOnlyList<DirectReport> directReports)
    {
        if (string.IsNullOrWhiteSpace(assigneeName))
            return null;

        // Match by full name
        var match = directReports.FirstOrDefault(dr =>
            dr.FullName.Equals(assigneeName, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private static Guid? FindProjectId(string? projectName, IReadOnlyList<Project> projects)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return null;

        // Match by project name
        var match = projects.FirstOrDefault(p =>
            p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private static int? ParseStoryPoints(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Handle decimal story points (e.g., "0.5" -> 1, "1.5" -> 2)
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doublePoints))
            return (int)Math.Ceiling(doublePoints);

        return null;
    }

    private static int? ParseTimeSpent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Jira time format: "1w 2d 3h 30m" or "2h 30m" or "45m" or seconds like "3600"
        var totalMinutes = 0;
        var lower = value.ToLowerInvariant().Trim();

        // Try parsing as pure number (seconds)
        if (int.TryParse(lower, out var seconds))
            return seconds / 60;

        // Parse Jira time format
        var parts = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.EndsWith("w") && int.TryParse(part.TrimEnd('w'), out var weeks))
                totalMinutes += weeks * 5 * 8 * 60; // 5 days * 8 hours
            else if (part.EndsWith("d") && int.TryParse(part.TrimEnd('d'), out var days))
                totalMinutes += days * 8 * 60; // 8 hours per day
            else if (part.EndsWith("h") && int.TryParse(part.TrimEnd('h'), out var hours))
                totalMinutes += hours * 60;
            else if (part.EndsWith("m") && int.TryParse(part.TrimEnd('m'), out var minutes))
                totalMinutes += minutes;
        }

        return totalMinutes > 0 ? totalMinutes : null;
    }

    private static string GetAllValues(Dictionary<string, string> data, string[] possibleKeys)
    {
        // Since MapRowToDictionary now aggregates duplicate columns, just get the value
        var value = GetValue(data, possibleKeys);
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Clean up: split by comma, trim, remove duplicates, rejoin
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct();

        return string.Join(",", parts);
    }

    private static DateTime? ParseDueDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Try common Jira date formats
        var formats = new[]
        {
            "dd/MMM/yy",           // 01/Jan/24
            "dd/MMM/yyyy",         // 01/Jan/2024
            "yyyy-MM-dd",          // 2024-01-01
            "MM/dd/yyyy",          // 01/01/2024
            "dd/MM/yyyy",          // 01/01/2024
            "yyyy/MM/dd"           // 2024/01/01
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
        }

        // Fallback to standard parsing
        if (DateTime.TryParse(value, out var parsedDate))
        {
            return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static void UpdateTaskStatus(TeamTask task, TaskStatus newStatus)
    {
        if (task.Status == newStatus)
            return;

        switch (newStatus)
        {
            case TaskStatus.Backlog:
                task.MoveToBacklog();
                break;

            case TaskStatus.Todo:
                task.MoveToTodo();
                break;

            case TaskStatus.Blocked:
                // Block method needs to be defined in TeamTask entity
                task.Block();
                break;

            case TaskStatus.InProgress:
                if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                    task.Start();
                break;

            case TaskStatus.InReview:
                if (task.Status == TaskStatus.InProgress || task.Status == TaskStatus.Blocked || task.Status == TaskStatus.InTest)
                    task.MoveToReview();
                else if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                {
                    task.Start();
                    task.MoveToReview();
                }
                break;

            case TaskStatus.InTest:
                // MoveToTest method needs to be defined in TeamTask entity
                task.MoveToTest();
                break;

            case TaskStatus.POAcceptance:
                // MoveToPOAcceptance method needs to be defined in TeamTask entity
                task.MoveToPOAcceptance();
                break;

            case TaskStatus.ReadyToRelease:
                // MoveToReadyToRelease method needs to be defined in TeamTask entity
                task.MoveToReadyToRelease();
                break;

            case TaskStatus.Done:
                if (task.Status != TaskStatus.Cancelled)
                    task.Complete();
                break;

            case TaskStatus.Cancelled:
                if (task.Status != TaskStatus.Done)
                    task.Cancel();
                break;
        }
    }

    /// <summary>
    /// Calculates estimated hours from story points using the app settings mapping.
    /// If story points don't match exactly, uses the next biggest mapping (or last one if none bigger).
    /// </summary>
    private static int? CalculateEstimatedHoursFromStoryPoints(int? storyPoints, AppSettingsDto appSettings)
    {
        if (!storyPoints.HasValue || storyPoints.Value <= 0)
            return null;

        if (appSettings.StoryPointMappings.Count == 0)
            return null;

        var sortedMappings = appSettings.StoryPointMappings.OrderBy(m => m.Points).ToList();

        // Find exact match first
        var exactMatch = sortedMappings.FirstOrDefault(m => m.Points == storyPoints.Value);
        if (exactMatch is not null)
            return (int)exactMatch.Hours;

        // Find next biggest mapping
        var nextBiggest = sortedMappings.FirstOrDefault(m => m.Points > storyPoints.Value);
        if (nextBiggest is not null)
            return (int)nextBiggest.Hours;

        // If no bigger mapping exists, use the last (maximum) mapping
        return (int)sortedMappings.Last().Hours;
    }

    private class TaskData
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public TaskType Type { get; init; }
        public TaskStatus Status { get; init; }
        public TaskPriority Priority { get; init; }
        public Guid? AssigneeId { get; init; }
        public Guid? ProjectId { get; init; }
        public int? StoryPoints { get; init; }
        public DateTime? DueDate { get; init; }
        public int? EstimatedHours { get; init; }
        public string Tags { get; init; } = string.Empty;
        public string Labels { get; init; } = string.Empty;
        public string Sprint { get; init; } = string.Empty;
        public int? TimeSpentMinutes { get; init; }
    }
}
