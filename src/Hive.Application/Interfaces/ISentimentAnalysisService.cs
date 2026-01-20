using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for sentiment analysis operations.
/// </summary>
public interface ISentimentAnalysisService
{
    /// <summary>
    /// Gets the sentiment analysis status (enabled, configured).
    /// </summary>
    Task<SentimentStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sentiment analysis for a specific direct report.
    /// </summary>
    Task<SentimentAnalysisDto?> GetForDirectReportAsync(
        Guid directReportId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team-wide sentiment overview.
    /// </summary>
    Task<TeamSentimentOverviewDto> GetTeamOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a Claude API key.
    /// </summary>
    Task<ApiKeyValidationResultDto> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
