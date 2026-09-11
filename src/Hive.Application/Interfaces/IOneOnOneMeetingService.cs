using Hive.Application.DTOs;

namespace Hive.Application.Interfaces;

/// <summary>
/// Service interface for 1:1 meetings, each of which is one markdown note.
/// </summary>
public interface IOneOnOneMeetingService
{
    Task<OneOnOneMeetingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<OneOnOneMeetingDto>> GetFilteredPagedAsync(MeetingPaginationParams pagination, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MeetingCountDto>> GetCountsAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> CreateBlankAsync(CreateBlankMeetingDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> UpdateContentAsync(Guid id, UpdateMeetingContentDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> UpdateTagsAsync(Guid id, UpdateMeetingTagsDto dto, CancellationToken cancellationToken = default);
    Task<OneOnOneMeetingDto> UpdateDateAsync(Guid id, UpdateMeetingDateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
