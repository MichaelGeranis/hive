namespace Hive.Application.DTOs;

/// <summary>
/// DTO for sentiment analysis results of a direct report.
/// </summary>
public record SentimentAnalysisDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public SentimentScoreDto Score { get; init; } = new();
    public IReadOnlyList<string> KeyThemes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SentimentTrendDto> Trend { get; init; } = Array.Empty<SentimentTrendDto>();
    public DateTime AnalyzedAt { get; init; }
    public int NotesAnalyzed { get; init; }
    public int DaysAnalyzed { get; init; }
}

/// <summary>
/// DTO for sentiment scores.
/// </summary>
public record SentimentScoreDto
{
    public double Positive { get; init; }
    public double Neutral { get; init; }
    public double Negative { get; init; }
    public string OverallSentiment { get; init; } = "Unknown";
}

/// <summary>
/// DTO for monthly sentiment trend data.
/// </summary>
public record SentimentTrendDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public double PositiveScore { get; init; }
    public double NeutralScore { get; init; }
    public double NegativeScore { get; init; }
    public int NotesCount { get; init; }
}

/// <summary>
/// DTO for team-wide sentiment overview.
/// </summary>
public record TeamSentimentOverviewDto
{
    public double AveragePositive { get; init; }
    public double AverageNeutral { get; init; }
    public double AverageNegative { get; init; }
    public string OverallTeamSentiment { get; init; } = "Unknown";
    public IReadOnlyList<DirectReportSentimentSummaryDto> ByDirectReport { get; init; } = Array.Empty<DirectReportSentimentSummaryDto>();
    public IReadOnlyList<string> CommonThemes { get; init; } = Array.Empty<string>();
    public int TotalNotesAnalyzed { get; init; }
    public int DirectReportsAnalyzed { get; init; }
}

/// <summary>
/// DTO for a direct report's sentiment summary in team overview.
/// </summary>
public record DirectReportSentimentSummaryDto
{
    public Guid DirectReportId { get; init; }
    public string DirectReportName { get; init; } = string.Empty;
    public string OverallSentiment { get; init; } = "Unknown";
    public double PositiveScore { get; init; }
    public int NotesCount { get; init; }
    public string TrendDirection { get; init; } = "Stable";
}

/// <summary>
/// DTO for sentiment analysis configuration status.
/// </summary>
public record SentimentStatusDto
{
    public bool IsEnabled { get; init; }
    public bool IsConfigured { get; init; }
    public int AnalysisDays { get; init; }
}

/// <summary>
/// DTO for API key validation request.
/// </summary>
public record ValidateApiKeyDto
{
    public string ApiKey { get; init; } = string.Empty;
}

/// <summary>
/// DTO for API key validation response.
/// </summary>
public record ApiKeyValidationResultDto
{
    public bool Valid { get; init; }
    public string? Error { get; init; }
}
