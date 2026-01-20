using System.Collections.Concurrent;
using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory repository for sentiment analysis cache.
/// </summary>
public class SentimentAnalysisCacheRepository : ISentimentAnalysisCacheRepository
{
    private readonly InMemoryDbContext _context;

    public SentimentAnalysisCacheRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<SentimentAnalysisCache?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var cache = _context.SentimentAnalysisCache.Values
            .FirstOrDefault(c => c.DirectReportId == directReportId);
        return Task.FromResult(cache);
    }

    public Task<IReadOnlyList<SentimentAnalysisCache>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var caches = _context.SentimentAnalysisCache.Values.ToList();
        return Task.FromResult<IReadOnlyList<SentimentAnalysisCache>>(caches);
    }

    public Task<SentimentAnalysisCache> AddAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default)
    {
        if (!_context.SentimentAnalysisCache.TryAdd(cache.Id, cache))
        {
            throw new InvalidOperationException($"Sentiment cache with ID {cache.Id} already exists.");
        }
        return Task.FromResult(cache);
    }

    public Task UpdateAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default)
    {
        _context.SentimentAnalysisCache[cache.Id] = cache;
        return Task.CompletedTask;
    }

    public Task DeleteByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var cacheToRemove = _context.SentimentAnalysisCache.Values
            .FirstOrDefault(c => c.DirectReportId == directReportId);

        if (cacheToRemove is not null)
        {
            _context.SentimentAnalysisCache.TryRemove(cacheToRemove.Id, out _);
        }

        return Task.CompletedTask;
    }
}
