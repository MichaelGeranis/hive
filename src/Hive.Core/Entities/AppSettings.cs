namespace Hive.Core.Entities;

/// <summary>
/// Represents application settings stored in the database.
/// </summary>
public class AppSettings
{
    public Guid Id { get; private set; }
    public string StoryPointMappings { get; private set; } = string.Empty;
    public string TshirtSizeMappings { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Sentiment Analysis settings
    public string? ClaudeApiKey { get; private set; }
    public int SentimentAnalysisDays { get; private set; } = 90;
    public bool SentimentAnalysisEnabled { get; private set; }

    // Sprint Import settings
    // If set, only sprints matching this team name will be imported
    // If null, all sprints matching the pattern will be imported
    public string? SprintTeamFilter { get; private set; }

    // Dashboard insight thresholds
    public int MaxInProgressTasks { get; private set; } = 2;
    public int MaxBlockedTasks { get; private set; } = 1;
    public int MaxInReviewTasks { get; private set; } = 1;
    public int MinProjectMembers { get; private set; } = 2;

    // Private constructor for EF Core / serialization
    private AppSettings() { }

    public AppSettings(string storyPointMappings)
    {
        Id = Guid.NewGuid();
        StoryPointMappings = storyPointMappings ?? "[]";
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStoryPointMappings(string storyPointMappings)
    {
        StoryPointMappings = storyPointMappings ?? "[]";
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTshirtSizeMappings(string tshirtSizeMappings)
    {
        TshirtSizeMappings = tshirtSizeMappings ?? "[]";
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateClaudeApiKey(string? apiKey)
    {
        ClaudeApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSentimentSettings(int days, bool enabled)
    {
        SentimentAnalysisDays = days > 0 ? days : 90;
        SentimentAnalysisEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasClaudeApiKey => !string.IsNullOrWhiteSpace(ClaudeApiKey);

    public void UpdateSprintTeamFilter(string? teamFilter)
    {
        SprintTeamFilter = string.IsNullOrWhiteSpace(teamFilter) ? null : teamFilter.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDashboardThresholds(int maxInProgress, int maxBlocked, int maxInReview, int minProjectMembers)
    {
        MaxInProgressTasks = maxInProgress > 0 ? maxInProgress : 2;
        MaxBlockedTasks = maxBlocked > 0 ? maxBlocked : 1;
        MaxInReviewTasks = maxInReview > 0 ? maxInReview : 1;
        MinProjectMembers = minProjectMembers > 0 ? minProjectMembers : 2;
        UpdatedAt = DateTime.UtcNow;
    }
}
