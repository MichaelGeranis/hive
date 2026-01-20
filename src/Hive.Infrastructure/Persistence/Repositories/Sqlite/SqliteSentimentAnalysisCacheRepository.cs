using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

/// <summary>
/// SQLite repository for sentiment analysis cache.
/// </summary>
public class SqliteSentimentAnalysisCacheRepository : ISentimentAnalysisCacheRepository
{
    private readonly HiveDbContext _context;

    public SqliteSentimentAnalysisCacheRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SentimentAnalysisCache?> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.SentimentAnalysisCache
            .FirstOrDefaultAsync(c => c.DirectReportId == directReportId, cancellationToken);
    }

    public async Task<IReadOnlyList<SentimentAnalysisCache>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SentimentAnalysisCache
            .OrderByDescending(c => c.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SentimentAnalysisCache> AddAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default)
    {
        await _context.SentimentAnalysisCache.AddAsync(cache, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cache;
    }

    public async Task UpdateAsync(SentimentAnalysisCache cache, CancellationToken cancellationToken = default)
    {
        _context.SentimentAnalysisCache.Update(cache);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var cache = await _context.SentimentAnalysisCache
            .FirstOrDefaultAsync(c => c.DirectReportId == directReportId, cancellationToken);

        if (cache is not null)
        {
            _context.SentimentAnalysisCache.Remove(cache);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
