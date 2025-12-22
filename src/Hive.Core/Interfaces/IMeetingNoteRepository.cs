using Hive.Core.Entities;

namespace Hive.Core.Interfaces;

/// <summary>
/// Repository interface for MeetingNote entity.
/// </summary>
public interface IMeetingNoteRepository
{
    Task<MeetingNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNote>> GetByMeetingIdAsync(Guid meetingId, bool includePrivate = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNote>> GetActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNote>> GetOpenActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNote>> GetOverdueActionItemsAsync(CancellationToken cancellationToken = default);
    Task<MeetingNote> AddAsync(MeetingNote note, CancellationToken cancellationToken = default);
    Task UpdateAsync(MeetingNote note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
