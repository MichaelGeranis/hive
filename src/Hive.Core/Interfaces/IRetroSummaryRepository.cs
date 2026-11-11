using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for the cached retro summary. There is at most one.
/// </summary>
public interface IRetroSummaryRepository
{
    /// <summary>
    /// Gets the cached retro summary, or null when none has been made yet.
    /// </summary>
    Task<RetroSummary?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every cached retro summary, for the backup.
    /// </summary>
    Task<IReadOnlyList<RetroSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RetroSummary> AddAsync(RetroSummary summary, CancellationToken cancellationToken = default);

    Task UpdateAsync(RetroSummary summary, CancellationToken cancellationToken = default);
}
