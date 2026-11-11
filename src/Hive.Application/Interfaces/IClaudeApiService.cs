using Hive.Core.Entities;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for interacting with the Claude API.
/// </summary>
public interface IClaudeApiService
{
    /// <summary>
    /// Analyzes the sentiment of 1:1 notes using the Claude API.
    /// </summary>
    /// <param name="apiKey">The manager's Claude API key.</param>
    /// <param name="notes">The 1:1 bodies to analyse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ClaudeApiException">Claude rejected the request, declined it, or could not be reached.</exception>
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        string apiKey,
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarises the team's retrospectives using the Claude API.
    /// </summary>
    /// <param name="apiKey">The manager's Claude API key.</param>
    /// <param name="retros">The retro notes to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ClaudeApiException">Claude rejected the request, declined it, or could not be reached.</exception>
    Task<RetroSummaryResult> SummarizeRetrosAsync(
        string apiKey,
        IReadOnlyList<RetroForAnalysis> retros,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reviews whether a task is ready to build, using the Claude API.
    /// </summary>
    /// <exception cref="ClaudeApiException">Claude rejected the request, declined it, or could not be reached.</exception>
    Task<TaskReviewResult> ReviewTaskAsync(
        string apiKey,
        TaskForReview task,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that an API key is accepted, without generating any tokens.
    /// </summary>
    Task<(bool IsValid, string? Error)> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// The AI model, whichever provider serves it, could not give an answer: for sentiment or for
/// the retro summary. The message is safe to show the manager.
/// </summary>
public abstract class AiProviderException : Exception
{
    protected AiProviderException(string message) : base(message)
    {
    }

    protected AiProviderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// The Claude API rejected a request, declined it, or could not be reached. The message is
/// safe to show the manager.
/// </summary>
public class ClaudeApiException : AiProviderException
{
    public ClaudeApiException(string message) : base(message)
    {
    }

    public ClaudeApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Represents the body of a 1:1 prepared for sentiment analysis.
/// </summary>
public record MeetingNoteForAnalysis(
    string Content,
    DateOnly MeetingDate
);

/// <summary>
/// Result of sentiment analysis, from whichever provider ran it.
/// </summary>
public record SentimentAnalysisResult(
    double Positive,
    double Neutral,
    double Negative,
    string OverallSentiment,
    IReadOnlyList<string> KeyThemes,
    IReadOnlyList<MonthlySentiment> MonthlyBreakdown,
    string Reasoning = ""
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

/// <summary>
/// The body of one retrospective prepared for the retro summary.
/// </summary>
public record RetroForAnalysis(
    string Content,
    DateOnly RetroDate
);

/// <summary>
/// The retro summary, from whichever provider wrote it.
/// </summary>
public record RetroSummaryResult(
    string Summary,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<RetroActionItem> ActionItems,
    IReadOnlyList<string> Suggestions
);

/// <summary>
/// Something the team agreed to do in a retro. The owner is empty when the notes name nobody.
/// </summary>
public record RetroActionItem(
    string Text,
    DateOnly? RetroDate,
    string Owner
);

/// <summary>
/// One task prepared for a readiness review: what it is called, its type, its description, and
/// whether Hive found acceptance criteria in it (a fact the model builds on, not re-judges).
/// </summary>
public record TaskForReview(
    string Title,
    string Type,
    string Description,
    bool HasAcceptanceCriteria = false
);

/// <summary>
/// A readiness review, from whichever provider made it.
/// </summary>
public record TaskReviewResult(
    TaskReadinessVerdict Verdict,
    string Summary,
    IReadOnlyList<string> Missing
);
