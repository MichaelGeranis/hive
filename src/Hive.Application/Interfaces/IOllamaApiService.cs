namespace Hive.Application.Interfaces;

/// <summary>
/// Sentiment analysis, the retro summary and task reviews on a model served by Ollama, usually on the
/// manager's own machine. Asks for exactly what <see cref="IClaudeApiService"/> asks for, so
/// the results compare.
/// </summary>
public interface IOllamaApiService
{
    /// <summary>
    /// Analyzes the sentiment of 1:1 notes with the given Ollama model.
    /// </summary>
    /// <exception cref="OllamaApiException">Ollama could not be reached, lacks the model, or gave no usable answer.</exception>
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
        OllamaConnection connection,
        IReadOnlyList<MeetingNoteForAnalysis> notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarises the team's retrospectives with the given Ollama model.
    /// </summary>
    /// <exception cref="OllamaApiException">Ollama could not be reached, lacks the model, or gave no usable answer.</exception>
    Task<RetroSummaryResult> SummarizeRetrosAsync(
        OllamaConnection connection,
        IReadOnlyList<RetroForAnalysis> retros,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reviews whether a task is ready to build, with the given Ollama model.
    /// </summary>
    /// <exception cref="OllamaApiException">Ollama could not be reached, lacks the model, or gave no usable answer.</exception>
    Task<TaskReviewResult> ReviewTaskAsync(
        OllamaConnection connection,
        TaskForReview task,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that Ollama answers at the address and has the model installed, without running it.
    /// </summary>
    Task<(bool IsValid, string? Error)> CheckConnectionAsync(
        OllamaConnection connection,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Where Ollama answers and which model to run, read from AppSettings.
/// </summary>
public record OllamaConnection(string BaseUrl, string Model);

/// <summary>
/// Ollama could not be reached, does not have the model, or gave no usable answer. The
/// message is safe to show the manager.
/// </summary>
public class OllamaApiException : AiProviderException
{
    public OllamaApiException(string message) : base(message)
    {
    }

    public OllamaApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
