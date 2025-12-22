using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for OneOnOneMeeting management.
/// </summary>
public class OneOnOneMeetingService : IOneOnOneMeetingService
{
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IMeetingNoteRepository _noteRepository;
    private readonly IDirectReportRepository _directReportRepository;

    public OneOnOneMeetingService(
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository noteRepository,
        IDirectReportRepository directReportRepository)
    {
        _meetingRepository = meetingRepository ?? throw new ArgumentNullException(nameof(meetingRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
    }

    public async Task<OneOnOneMeetingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _meetingRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<OneOnOneMeetingDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _meetingRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        var meetingDto = await MapToDtoAsync(entity, cancellationToken);
        var notes = await _noteRepository.GetByMeetingIdAsync(id, true, cancellationToken);
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        var noteDtos = notes.Select(n => MapNoteToDto(n, entity.ScheduledDate, directReport?.FullName ?? "Unknown")).ToList();

        return new OneOnOneMeetingDetailsDto
        {
            Meeting = meetingDto,
            Notes = noteDtos
        };
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetAllAsync(cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetByStatusAsync(MeetingStatus status, CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetByStatusAsync(status, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetUpcomingAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetUpcomingAsync(days, cancellationToken);
        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto?> GetNextMeetingAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entity = await _meetingRepository.GetNextMeetingAsync(directReportId, cancellationToken);
        if (entity is null) return null;

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> CreateAsync(CreateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        }

        var entity = new OneOnOneMeeting(
            dto.DirectReportId,
            dto.ScheduledDate,
            dto.DurationMinutes,
            dto.Location,
            dto.Agenda);

        var created = await _meetingRepository.AddAsync(entity, cancellationToken);
        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> UpdateAsync(Guid id, UpdateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateDetails(dto.ScheduledDate, dto.DurationMinutes, dto.Location, dto.Agenda);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Complete();
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Cancel();
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> RescheduleAsync(Guid id, RescheduleMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.Reschedule(dto.NewDate);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _meetingRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException(nameof(OneOnOneMeeting), id);
        }

        await _meetingRepository.DeleteAsync(id, cancellationToken);
    }

    private async Task<OneOnOneMeeting> GetEntityOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _meetingRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(OneOnOneMeeting), id);
        }
        return entity;
    }

    private async Task<OneOnOneMeetingDto> MapToDtoAsync(OneOnOneMeeting entity, CancellationToken cancellationToken)
    {
        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);
        var notes = await _noteRepository.GetByMeetingIdAsync(entity.Id, true, cancellationToken);
        var openActionItems = notes.Count(n => n.Category == NoteCategory.ActionItem
                                                && n.ActionStatus != ActionItemStatus.Completed
                                                && n.ActionStatus != ActionItemStatus.Cancelled);

        return new OneOnOneMeetingDto
        {
            Id = entity.Id,
            DirectReportId = entity.DirectReportId,
            DirectReportName = directReport?.FullName ?? "Unknown",
            ScheduledDate = entity.ScheduledDate,
            DurationMinutes = entity.DurationMinutes,
            Location = entity.Location,
            Agenda = entity.Agenda,
            Status = entity.Status,
            StatusName = GetStatusName(entity.Status),
            CompletedAt = entity.CompletedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            NoteCount = notes.Count,
            OpenActionItemCount = openActionItems
        };
    }

    private async Task<IReadOnlyList<OneOnOneMeetingDto>> MapToDtosAsync(IReadOnlyList<OneOnOneMeeting> entities, CancellationToken cancellationToken)
    {
        var result = new List<OneOnOneMeetingDto>();
        foreach (var entity in entities)
        {
            result.Add(await MapToDtoAsync(entity, cancellationToken));
        }
        return result;
    }

    private static MeetingNoteDto MapNoteToDto(MeetingNote note, DateTime meetingDate, string directReportName) => new()
    {
        Id = note.Id,
        MeetingId = note.MeetingId,
        MeetingDate = meetingDate,
        DirectReportName = directReportName,
        Content = note.Content,
        Category = note.Category,
        CategoryName = GetCategoryName(note.Category),
        IsPrivate = note.IsPrivate,
        ActionStatus = note.ActionStatus,
        ActionStatusName = note.ActionStatus.HasValue ? GetActionStatusName(note.ActionStatus.Value) : null,
        ActionDueDate = note.ActionDueDate,
        ActionAssignee = note.ActionAssignee,
        IsOverdue = note.IsOverdue(),
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt
    };

    private static string GetStatusName(MeetingStatus status) => status switch
    {
        MeetingStatus.Scheduled => "Scheduled",
        MeetingStatus.Completed => "Completed",
        MeetingStatus.Cancelled => "Cancelled",
        MeetingStatus.Rescheduled => "Rescheduled",
        _ => "Unknown"
    };

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
