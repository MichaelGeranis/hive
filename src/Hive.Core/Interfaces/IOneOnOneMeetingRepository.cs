using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for OneOnOneMeeting entity.
/// </summary>
public interface IOneOnOneMeetingRepository
{
    Task<OneOnOneMeeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeeting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeeting>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists 1:1s newest first, optionally limited to one person, to the unlinked ones,
    /// or to those matching a search term.
    /// </summary>
    Task<(IReadOnlyList<OneOnOneMeeting> Items, int TotalCount)> GetFilteredPagedAsync(
        int skip,
        int take,
        Guid? directReportId,
        bool unlinkedOnly,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the 1:1s held with each person. Unlinked 1:1s are reported under a null id.
    /// </summary>
    Task<IReadOnlyList<(Guid? DirectReportId, int Count)>> GetCountsByDirectReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Total number of 1:1s logged.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<OneOnOneMeeting> AddAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default);
    Task UpdateAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
