using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using static Hive.Application.Services.JiraTaskRules;
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
    private static readonly string[] SprintColumns = { "Sprint" };
    private static readonly string[] LabelsColumns = { "Labels", "Label" };
    private static readonly string[] ComponentsColumns = { "Component/s", "Components", "Component" };
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
                var sprintTeamFilter = appSettings.SprintTeamFilter;
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
                var taskData = MapJiraRowToTask(rowData, directReports, projects, out var projectName);

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
                            var parent = await _parentService.GetOrCreateAsync(parentName, cancellationToken: cancellationToken);
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
                    ApplyToExisting(existingTask, taskData, parentId);

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
                    var newTask = CreateTask(taskData, parentId);

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
                        await _parentService.GetOrCreateAsync(taskData.Title, taskId, cancellationToken);
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

    private static JiraTaskData MapJiraRowToTask(
        Dictionary<string, string> rowData,
        IReadOnlyList<DirectReport> directReports,
        IReadOnlyList<Project> projects,
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

        // Collect all Labels, Components, and Sprint values (Jira exports multiple columns with same name)
        var labels = GetAllValues(rowData, LabelsColumns);
        var components = GetAllValues(rowData, ComponentsColumns);
        var sprints = GetAllValues(rowData, SprintColumns);

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

        return new JiraTaskData
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
            Tags = tags,
            Labels = labels,
            Components = components,
            Sprint = sprints,
        };
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

}
