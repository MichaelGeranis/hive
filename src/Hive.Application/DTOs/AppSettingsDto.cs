namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for AppSettings.
/// </summary>
public record AppSettingsDto
{
    public Guid Id { get; init; }
    public List<TshirtSizeMapping> TshirtSizeMappings { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Sentiment Analysis settings (note: API key is not exposed, only a flag)
    // "Ollama" or "Claude"
    public string SentimentProvider { get; init; } = string.Empty;
    public bool HasClaudeApiKey { get; init; }
    public string OllamaBaseUrl { get; init; } = string.Empty;
    public string OllamaModel { get; init; } = string.Empty;
    public int SentimentAnalysisDays { get; init; }
    public bool SentimentAnalysisEnabled { get; init; }

    // Retro summary settings: the same model as sentiment
    public bool RetroSummaryEnabled { get; init; }
    public int RetroSummaryCount { get; init; }

    // Task readiness: an AI review of upcoming tasks, with the same model
    public bool TaskReviewEnabled { get; init; }

    // Sprint Import settings
    public string? SprintTeamFilter { get; init; }

    // Dashboard insight thresholds
    public int MaxInProgressTasks { get; init; }
    public int MaxBlockedTasks { get; init; }
    public int MaxInReviewTasks { get; init; }
    public int MinProductMembers { get; init; }

    // Fewest products a direct report should have worked on
    public int MinMemberProducts { get; init; }

    // Days between 1:1s the manager aims for with each direct report
    public int OneOnOneCadenceDays { get; init; }

    // Days an in-flight task may keep one status before it is reported as stale
    public int StaleTaskDays { get; init; }

    // Maintenance label configuration
    public List<string> MaintenanceLabels { get; init; } = new();

    // The epic names that name each work category, by category (TechMaintenance, TechRoadmap,
    // TechDebt, SystemImprovements); every other epic is Productisation
    public Dictionary<string, List<string>> WorkCategoryEpicNames { get; init; } = new();

    // Jira integration (note: API token is not exposed, only a flag)
    public string? JiraBaseUrl { get; init; }
    // Append "/{issue key}" to open an issue in Jira
    public string? JiraBrowseUrl { get; init; }
    public string? JiraEmail { get; init; }
    public bool HasJiraApiToken { get; init; }
    public int? JiraBoardId { get; init; }
    public string? JiraProjectKey { get; init; }

    // Google Calendar (note: the secret address is not exposed, only a flag)
    public bool HasGoogleCalendarUrl { get; init; }
}

public record TshirtSizeMapping
{
    public string Size { get; init; } = string.Empty;
    public decimal Sprints { get; init; }
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// DTO for updating AppSettings.
/// </summary>
public record UpdateAppSettingsDto
{
    public List<TshirtSizeMapping> TshirtSizeMappings { get; init; } = new();

    // Sentiment Analysis settings
    // "Ollama" or "Claude"; null leaves it unchanged
    public string? SentimentProvider { get; init; }
    public string? ClaudeApiKey { get; init; }
    // Null leaves it unchanged; an empty string goes back to the default
    public string? OllamaBaseUrl { get; init; }
    public string? OllamaModel { get; init; }
    public int? SentimentAnalysisDays { get; init; }
    public bool? SentimentAnalysisEnabled { get; init; }

    // Retro summary settings; null leaves them unchanged
    public bool? RetroSummaryEnabled { get; init; }
    public int? RetroSummaryCount { get; init; }
    public bool? TaskReviewEnabled { get; init; }

    // Sprint Import settings
    // Set to a team name to filter imports, or null/empty to disable filtering
    public string? SprintTeamFilter { get; init; }
    // Explicit flag to clear the team filter (set to true with null SprintTeamFilter to clear)
    public bool ClearSprintTeamFilter { get; init; }

    // Dashboard insight thresholds
    public int? MaxInProgressTasks { get; init; }
    public int? MaxBlockedTasks { get; init; }
    public int? MaxInReviewTasks { get; init; }
    public int? MinProductMembers { get; init; }
    public int? MinMemberProducts { get; init; }
    public int? OneOnOneCadenceDays { get; init; }
    public int? StaleTaskDays { get; init; }

    // Maintenance label configuration
    public List<string>? MaintenanceLabels { get; init; }

    // Null leaves them unchanged; a category left out has no names
    public Dictionary<string, List<string>>? WorkCategoryEpicNames { get; init; }

    // Jira integration
    public string? JiraBaseUrl { get; init; }
    // Null leaves the value unchanged; an empty string clears it
    public string? JiraEmail { get; init; }
    public string? JiraApiToken { get; init; }
    public int? JiraBoardId { get; init; }
    // Explicit flag to clear the board ID (set to true with null JiraBoardId to clear)
    public bool ClearJiraBoardId { get; init; }
    // Null leaves the value unchanged; an empty string clears it
    public string? JiraProjectKey { get; init; }

    // Google Calendar's secret iCal address. Null leaves it unchanged; an empty string clears it
    public string? GoogleCalendarUrl { get; init; }
}
