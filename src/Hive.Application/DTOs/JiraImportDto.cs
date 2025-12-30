namespace Hive.Application.DTOs;

/// <summary>
/// DTO for importing tasks from Jira CSV.
/// </summary>
public record JiraImportRequestDto
{
    public string CsvContent { get; init; } = string.Empty;
    public bool UpdateExisting { get; init; } = true;
    public string MatchField { get; init; } = "IssueKey"; // IssueKey or Title
}

/// <summary>
/// Result of a Jira CSV import operation.
/// </summary>
public record JiraImportResultDto
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public List<JiraImportedTaskDto> ImportedTasks { get; init; } = new();
}

/// <summary>
/// Represents a task imported from Jira.
/// </summary>
public record JiraImportedTaskDto
{
    public Guid? TaskId { get; init; }
    public string IssueKey { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public bool IsNew { get; init; }
    public bool IsUpdated { get; init; }
}

/// <summary>
/// Preview of Jira CSV import before actual import.
/// </summary>
public record JiraImportPreviewDto
{
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public List<string> DetectedColumns { get; init; } = new();
    public List<string> MappingWarnings { get; init; } = new();
    public List<JiraImportPreviewRowDto> SampleRows { get; init; } = new();
}

/// <summary>
/// Preview of a single row from Jira CSV.
/// </summary>
public record JiraImportPreviewRowDto
{
    public int RowNumber { get; init; }
    public string IssueKey { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Assignee { get; init; } = string.Empty;
    public string? StoryPoints { get; init; }
    public bool IsValid { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
}
