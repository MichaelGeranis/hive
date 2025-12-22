using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for MeetingNote management.
/// </summary>
public interface IMeetingNoteService
{
    Task<MeetingNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNoteDto>> GetByMeetingIdAsync(Guid meetingId, bool includePrivate = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNoteDto>> GetActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNoteDto>> GetOpenActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingNoteDto>> GetOverdueActionItemsAsync(CancellationToken cancellationToken = default);
    Task<MeetingNoteDto> CreateAsync(CreateMeetingNoteDto dto, CancellationToken cancellationToken = default);
    Task<MeetingNoteDto> UpdateAsync(Guid id, UpdateMeetingNoteDto dto, CancellationToken cancellationToken = default);
    Task<MeetingNoteDto> UpdateActionStatusAsync(Guid id, UpdateActionStatusDto dto, CancellationToken cancellationToken = default);
    Task<MeetingNoteDto> CompleteActionAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
