using System.Globalization;
using System.Text;
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

    public JiraImportService(
        ITeamTaskRepository taskRepository,
        IDirectReportRepository directReportRepository,
        IProjectRepository projectRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
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

        // Preview first 10 rows
        for (int i = 1; i < Math.Min(11, lines.Count); i++)
        {
            var values = ParseCsvRow(lines[i]);
            var rowData = MapRowToDictionary(headers, values);
            var previewRow = CreatePreviewRow(i + 1, rowData);

            sampleRows.Add(previewRow);
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

        // Load all existing tasks, direct reports, and projects once
        var existingTasks = await _taskRepository.GetAllAsync(cancellationToken);
        var directReports = await _directReportRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);

        var totalRows = lines.Count - 1;
        var successCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

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

                // Map fields
                var taskData = await MapJiraRowToTaskAsync(rowData, directReports, projects, cancellationToken);

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
                        taskData.Tags);

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
                        taskData.Tags);

                    // Set status
                    UpdateTaskStatus(newTask, taskData.Status);

                    var created = await _taskRepository.AddAsync(newTask, cancellationToken);

                    importedTasks.Add(new JiraImportedTaskDto
                    {
                        TaskId = created.Id,
                        IssueKey = issueKey ?? "N/A",
                        Summary = summary ?? "N/A",
                        IsNew = true,
                        IsUpdated = false
                    });
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
            result[headers[i]] = values[i];
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

    private Task<TaskData> MapJiraRowToTaskAsync(
        Dictionary<string, string> rowData,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Project> projects,
        CancellationToken cancellationToken)
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
        var dueDateStr = GetValue(rowData, DueDateColumns);

        // Map fields
        var taskType = MapIssueTypeToTaskType(issueType);
        var taskStatus = MapJiraStatusToTaskStatus(status);
        var taskPriority = MapJiraPriorityToTaskPriority(priority);
        var assigneeId = FindAssigneeId(assigneeName, directReports);
        var projectId = FindProjectId(projectName, projects);
        var storyPoints = ParseStoryPoints(storyPointsStr);
        var dueDate = ParseDueDate(dueDateStr);

        // Build tags with Jira Issue Key
        var tags = string.IsNullOrWhiteSpace(issueKey)
            ? "imported-from-jira"
            : $"jira:{issueKey},imported-from-jira";

        return Task.FromResult(new TaskData
        {
            Title = summary,
            Description = description,
            Type = taskType,
            Status = taskStatus,
            Priority = taskPriority,
            AssigneeId = assigneeId,
            ProjectId = projectId,
            StoryPoints = storyPoints,
            DueDate = dueDate,
            Tags = tags
        });
    }

    private static TaskType MapIssueTypeToTaskType(string? issueType)
    {
        if (string.IsNullOrWhiteSpace(issueType))
            return TaskType.Task;

        return issueType.ToLowerInvariant() switch
        {
            "bug" => TaskType.Bug,
            "story" => TaskType.Feature,
            "feature" => TaskType.Feature,
            "epic" => TaskType.Feature,
            "improvement" => TaskType.Improvement,
            "enhancement" => TaskType.Improvement,
            "research" => TaskType.Research,
            "spike" => TaskType.Research,
            "documentation" => TaskType.Documentation,
            "docs" => TaskType.Documentation,
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
            "backlog" => TaskStatus.Backlog,
            "todo" => TaskStatus.Todo,
            "to do" => TaskStatus.Todo,
            "toDo" => TaskStatus.Todo,
            "selected for development" => TaskStatus.Todo,
            "selectedfordevelopment" => TaskStatus.Todo,
            "inprogress" => TaskStatus.InProgress,
            "in progress" => TaskStatus.InProgress,
            "inProgress" => TaskStatus.InProgress,
            "inreview" => TaskStatus.InReview,
            "in review" => TaskStatus.InReview,
            "inReview" => TaskStatus.InReview,
            "review" => TaskStatus.InReview,
            "code review" => TaskStatus.InReview,
            "codeReview" => TaskStatus.InReview,
            "done" => TaskStatus.Done,
            "closed" => TaskStatus.Done,
            "resolved" => TaskStatus.Done,
            "complete" => TaskStatus.Done,
            "completed" => TaskStatus.Done,
            "cancelled" => TaskStatus.Cancelled,
            "canceled" => TaskStatus.Cancelled,
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

        if (int.TryParse(value, out var points))
            return points;

        return null;
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
            case TaskStatus.InProgress:
                if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                    task.Start();
                break;
            case TaskStatus.InReview:
                if (task.Status == TaskStatus.InProgress)
                    task.MoveToReview();
                else if (task.Status != TaskStatus.Done && task.Status != TaskStatus.Cancelled)
                {
                    task.Start();
                    task.MoveToReview();
                }
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
    }
}
