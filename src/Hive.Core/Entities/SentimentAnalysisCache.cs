namespace Hive.Core.Entities;

/// <summary>
/// Caches sentiment analysis results for a direct report to avoid repeated API calls.
/// </summary>
public class SentimentAnalysisCache
{
    public Guid Id { get; private set; }
    public Guid DirectReportId { get; private set; }

    // Sentiment scores (0-100)
    public double PositiveScore { get; private set; }
    public double NeutralScore { get; private set; }
    public double NegativeScore { get; private set; }
    public string OverallSentiment { get; private set; } = string.Empty;

    // Analysis details stored as JSON
    public string KeyThemesJson { get; private set; } = "[]";
    public string TrendDataJson { get; private set; } = "[]";

    /// <summary>
    /// The model's own explanation of its judgement, in a few sentences. Empty for a result
    /// cached before Hive asked for one.
    /// </summary>
    public string Reasoning { get; private set; } = string.Empty;

    public const int MaxReasoningLength = 2000;

    // Metadata
    public int NotesAnalyzed { get; private set; }
    public int DaysAnalyzed { get; private set; }
    public DateTime LatestNoteDate { get; private set; }
    public DateTime AnalyzedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private SentimentAnalysisCache() { }

    public SentimentAnalysisCache(
        Guid directReportId,
        double positiveScore,
        double neutralScore,
        double negativeScore,
        string overallSentiment,
        string keyThemesJson,
        string trendDataJson,
        int notesAnalyzed,
        int daysAnalyzed,
        DateTime latestNoteDate,
        string? reasoning = null)
    {
        if (directReportId == Guid.Empty)
            throw new ArgumentException("Direct report ID cannot be empty.", nameof(directReportId));

        Id = Guid.NewGuid();
        DirectReportId = directReportId;
        PositiveScore = Math.Clamp(positiveScore, 0, 100);
        NeutralScore = Math.Clamp(neutralScore, 0, 100);
        NegativeScore = Math.Clamp(negativeScore, 0, 100);
        OverallSentiment = overallSentiment ?? "Unknown";
        KeyThemesJson = keyThemesJson ?? "[]";
        TrendDataJson = trendDataJson ?? "[]";
        NotesAnalyzed = notesAnalyzed;
        DaysAnalyzed = daysAnalyzed;
        LatestNoteDate = latestNoteDate;
        Reasoning = NormalizeReasoning(reasoning);
        AnalyzedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the cached analysis with new results.
    /// </summary>
    public void Update(
        double positiveScore,
        double neutralScore,
        double negativeScore,
        string overallSentiment,
        string keyThemesJson,
        string trendDataJson,
        int notesAnalyzed,
        int daysAnalyzed,
        DateTime latestNoteDate,
        string? reasoning = null)
    {
        PositiveScore = Math.Clamp(positiveScore, 0, 100);
        NeutralScore = Math.Clamp(neutralScore, 0, 100);
        NegativeScore = Math.Clamp(negativeScore, 0, 100);
        OverallSentiment = overallSentiment ?? "Unknown";
        KeyThemesJson = keyThemesJson ?? "[]";
        TrendDataJson = trendDataJson ?? "[]";
        NotesAnalyzed = notesAnalyzed;
        DaysAnalyzed = daysAnalyzed;
        LatestNoteDate = latestNoteDate;
        Reasoning = NormalizeReasoning(reasoning);
        AnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the cache is stale: a 1:1 newer than the ones analysed, or a result cached
    /// before Hive asked for the reasoning, which is analysed again once to get it.
    /// </summary>
    public bool IsStale(DateTime currentLatestNoteDate)
    {
        return currentLatestNoteDate > LatestNoteDate || Reasoning.Length == 0;
    }

    private static string NormalizeReasoning(string? reasoning)
    {
        var text = reasoning?.Trim() ?? string.Empty;
        return text.Length <= MaxReasoningLength ? text : text[..(MaxReasoningLength - 1)].TrimEnd() + "…";
    }
}
