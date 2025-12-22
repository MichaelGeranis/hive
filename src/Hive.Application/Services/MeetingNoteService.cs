using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for MeetingNote management.
/// </summary>
public class MeetingNoteService : IMeetingNoteService
{
    private readonly IMeetingNoteRepository _noteRepository;
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IDirectReportRepository _directReportRepository;

    public MeetingNoteService(
        IMeetingNoteRepository noteRepository,
        IOneOnOneMeetingRepository meetingRepository,
        IDirectReportRepository directReportRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _meetingRepository = meetingRepository ?? throw new ArgumentNullException(nameof(meetingRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
    }

    public async Task<MeetingNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _noteRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNoteDto>> GetByMeetingIdAsync(Guid meetingId, bool includePrivate = true, CancellationToken cancellationToken = default)
    {
        var notes = await _noteRepository.GetByMeetingIdAsync(meetingId, includePrivate, cancellationToken);
        return await MapToDtosAsync(notes, cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNoteDto>> GetActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var notes = await _noteRepository.GetActionItemsAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(notes, cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNoteDto>> GetOpenActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var notes = await _noteRepository.GetOpenActionItemsAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(notes, cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNoteDto>> GetOverdueActionItemsAsync(CancellationToken cancellationToken = default)
    {
        var notes = await _noteRepository.GetOverdueActionItemsAsync(cancellationToken);
        return await MapToDtosAsync(notes, cancellationToken);
    }

    public async Task<MeetingNoteDto> CreateAsync(CreateMeetingNoteDto dto, CancellationToken cancellationToken = default)
    {
        var meeting = await _meetingRepository.GetByIdAsync(dto.MeetingId, cancellationToken);
        if (meeting is null)
        {
            throw new NotFoundException(nameof(OneOnOneMeeting), dto.MeetingId);
        }

        var entity = new MeetingNote(dto.MeetingId, dto.Content, dto.Category, dto.IsPrivate);

        if (dto.Category == NoteCategory.ActionItem)
        {
            entity.SetActionDetails(dto.ActionDueDate, dto.ActionAssignee);
        }

        var created = await _noteRepository.AddAsync(entity, cancellationToken);
        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<MeetingNoteDto> UpdateAsync(Guid id, UpdateMeetingNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateContent(dto.Content, dto.Category, dto.IsPrivate);

        if (dto.Category == NoteCategory.ActionItem)
        {
            entity.SetActionDetails(dto.ActionDueDate, dto.ActionAssignee);
        }

        await _noteRepository.UpdateAsync(entity, cancellationToken);
        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<MeetingNoteDto> UpdateActionStatusAsync(Guid id, UpdateActionStatusDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateActionStatus(dto.Status);
        await _noteRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<MeetingNoteDto> CompleteActionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.CompleteAction();
        await _noteRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _noteRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(MeetingNote), id);
        }

        await _noteRepository.DeleteAsync(id, cancellationToken);
    }

    private async Task<MeetingNote> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _noteRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(MeetingNote), id);
        }
        return entity;
    }

    private async Task<MeetingNoteDto> MapToDtoAsync(MeetingNote entity, CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetByIdAsync(entity.MeetingId, cancellationToken);
        var directReport = meeting is not null
            ? await _directReportRepository.GetByIdAsync(meeting.DirectReportId, cancellationToken)
            : null;

        return new MeetingNoteDto
        {
            Id = entity.Id,
            MeetingId = entity.MeetingId,
            MeetingDate = meeting?.ScheduledDate ?? DateTime.MinValue,
            DirectReportName = directReport?.FullName ?? "Unknown",
            Content = entity.Content,
            Category = entity.Category,
            CategoryName = GetCategoryName(entity.Category),
            IsPrivate = entity.IsPrivate,
            ActionStatus = entity.ActionStatus,
            ActionStatusName = entity.ActionStatus.HasValue ? GetActionStatusName(entity.ActionStatus.Value) : null,
            ActionDueDate = entity.ActionDueDate,
            ActionAssignee = entity.ActionAssignee,
            IsOverdue = entity.IsOverdue(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<IReadOnlyList<MeetingNoteDto>> MapToDtosAsync(IReadOnlyList<MeetingNote> entities, CancellationToken cancellationToken)
    {
        var result = new List<MeetingNoteDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken));
        }
        return result;
    }

    private static string GetCategoryName(NoteCategory category) => category switch
    {
        NoteCategory.Discussion => "Discussion",
        NoteCategory.ActionItem => "Action Item",
        NoteCategory.Feedback => "Feedback",
        NoteCategory.CareerDevelopment => "Career Development",
        NoteCategory.Blocker => "Blocker",
        NoteCategory.Achievement => "Achievement",
        NoteCategory.Personal => "Personal",
        NoteCategory.FollowUp => "Follow Up",
        _ => "Unknown"
    };

    private static string GetActionStatusName(ActionItemStatus status) => status switch
    {
        ActionItemStatus.Open => "Open",
        ActionItemStatus.InProgress => "In Progress",
        ActionItemStatus.Completed => "Completed",
        ActionItemStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };
}
