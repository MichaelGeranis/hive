using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory repository for the cached retro summary.
/// </summary>
public class RetroSummaryRepository : IRetroSummaryRepository
{
    private readonly InMemoryDbContext _context;

    public RetroSummaryRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<RetroSummary?> GetAsync(CancellationToken cancellationToken = default)
    {
        // Only one is ever made; after a restore the newest wins, as in SQLite
        var summary = _context.RetroSummaries.Values
            .OrderByDescending(s => s.AnalyzedAt)
            .FirstOrDefault();
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<RetroSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var summaries = _context.RetroSummaries.Values
            .OrderByDescending(s => s.AnalyzedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<RetroSummary>>(summaries);
    }

    public Task<RetroSummary> AddAsync(RetroSummary summary, CancellationToken cancellationToken = default)
    {
        if (!_context.RetroSummaries.TryAdd(summary.Id, summary))
        {
            throw new InvalidOperationException($"Retro summary with ID {summary.Id} already exists.");
        }
        return Task.FromResult(summary);
    }

    public Task UpdateAsync(RetroSummary summary, CancellationToken cancellationToken = default)
    {
        _context.RetroSummaries[summary.Id] = summary;
        return Task.CompletedTask;
    }
}
