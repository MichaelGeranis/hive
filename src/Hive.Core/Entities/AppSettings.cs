namespace Hive.Core.Entities;

/// <summary>
/// Represents application settings stored in the database.
/// </summary>
public class AppSettings
{
    public Guid Id { get; private set; }
    public string StoryPointMappings { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Sentiment Analysis settings
    public string? ClaudeApiKey { get; private set; }
    public int SentimentAnalysisDays { get; private set; } = 90;
    public bool SentimentAnalysisEnabled { get; private set; }

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
}
