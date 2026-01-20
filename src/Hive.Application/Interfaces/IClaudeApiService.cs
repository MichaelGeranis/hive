namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for interacting with the Claude API.
/// </summary>
public interface IClaudeApiService
{
    /// <summary>
    /// Analyzes the sentiment of meeting notes using Claude API.
    /// </summary>
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an API key is valid by making a test request.
    /// </summary>
    Task<(bool IsValid, string? Error)> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a meeting note prepared for sentiment analysis.
/// </summary>
public record MeetingNoteForAnalysis(
    string Content,
    string Category,
    DateTime MeetingDate
);

/// <summary>
/// Result of sentiment analysis from Claude API.
/// </summary>
public record SentimentAnalysisResult(
    double Positive,
    double Neutral,
    double Negative,
    string OverallSentiment,
    IReadOnlyList<string> KeyThemes,
    IReadOnlyList<MonthlySentiment> MonthlyBreakdown
);

/// <summary>
/// Monthly sentiment breakdown.
/// </summary>
public record MonthlySentiment(
    int Year,
    int Month,
    double Positive,
    double Neutral,
    double Negative,
    int NotesCount
);
