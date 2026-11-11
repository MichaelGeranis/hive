using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for sentiment analysis cache operations.
/// </summary>
public interface ISentimentAnalysisCacheRepository
{
    /// <summary>
    /// Gets the cached sentiment analysis for a direct report.
    /// </summary>
    Task<SentimentAnalysisCache?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cached sentiment analyses.
    /// </summary>
    Task<IReadOnlyList<SentimentAnalysisCache>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new sentiment analysis cache entry.
    /// </summary>
    Task<SentimentAnalysisCache> AddAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sentiment analysis cache entry.
    /// </summary>
    Task UpdateAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the cached sentiment analysis for a direct report.
    /// </summary>
    Task DeleteByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
}
