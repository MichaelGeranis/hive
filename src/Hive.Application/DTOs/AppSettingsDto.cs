namespace Hive.Application.DTOs;

/// <summary>
/// Data Transfer Object for AppSettings.
/// </summary>
public record AppSettingsDto
{
    public Guid Id { get; init; }
    public List<StoryPointMapping> StoryPointMappings { get; init; } = new();
    public List<TshirtSizeMapping> TshirtSizeMappings { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Sentiment Analysis settings (note: API key is not exposed, only a flag)
    public bool HasClaudeApiKey { get; init; }
    public int SentimentAnalysisDays { get; init; }
    public bool SentimentAnalysisEnabled { get; init; }

    // Sprint Import settings
    public string? SprintTeamFilter { get; init; }

    // Dashboard insight thresholds
    public int MaxInProgressTasks { get; init; }
    public int MaxBlockedTasks { get; init; }
    public int MaxInReviewTasks { get; init; }
    public int MinProjectMembers { get; init; }

    // Support & Maintenance label configuration
    public List<string> SupportLabels { get; init; } = new();
    public List<string> MaintenanceLabels { get; init; } = new();
}

public record StoryPointMapping
{
    public int Points { get; init; }
    public decimal Hours { get; init; }
    public string Label { get; init; } = string.Empty;
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
    public List<StoryPointMapping> StoryPointMappings { get; init; } = new();
    public List<TshirtSizeMapping> TshirtSizeMappings { get; init; } = new();

    // Sentiment Analysis settings
    public string? ClaudeApiKey { get; init; }
    public int? SentimentAnalysisDays { get; init; }
    public bool? SentimentAnalysisEnabled { get; init; }

    // Sprint Import settings
    // Set to a team name to filter imports, or null/empty to disable filtering
    public string? SprintTeamFilter { get; init; }
    // Explicit flag to clear the team filter (set to true with null SprintTeamFilter to clear)
    public bool ClearSprintTeamFilter { get; init; }

    // Dashboard insight thresholds
    public int? MaxInProgressTasks { get; init; }
    public int? MaxBlockedTasks { get; init; }
    public int? MaxInReviewTasks { get; init; }
    public int? MinProjectMembers { get; init; }

    // Support & Maintenance label configuration
    public List<string>? SupportLabels { get; init; }
    public List<string>? MaintenanceLabels { get; init; }
}
