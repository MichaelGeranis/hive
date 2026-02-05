using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for OneOnOneMeeting management.
/// Simplified for note tracking - no scheduling workflow.
/// </summary>
public class OneOnOneMeetingService : IOneOnOneMeetingService
{
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IMeetingNoteRepository _noteRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    public OneOnOneMeetingService(
        IOneOnOneMeetingRepository meetingRepository,
        IMeetingNoteRepository noteRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _meetingRepository = meetingRepository ?? throw new ArgumentNullException(nameof(meetingRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
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

        var noteDtos = notes.Select(n => MapNoteToDto(n, entity.MeetingDate, directReport?.FullName ?? "Unknown")).ToList();

        return new OneOnOneMeetingDetailsDto
        {
            Meeting = meetingDto,
            Notes = noteDtos
        };
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetAllAsync(cancellationToken);
        // Order by meeting date descending (most recent first)
        var ordered = entities.OrderByDescending(e => e.MeetingDate).ToList();
        return await MapToDtosAsync(ordered, cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        // Order by meeting date descending (most recent first)
        var ordered = entities.OrderByDescending(e => e.MeetingDate).ToList();
        return await MapToDtosAsync(ordered, cancellationToken);
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
            dto.MeetingDate,
            dto.Location,
            dto.Agenda);

        var created = await _meetingRepository.AddAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Created,
            EntityType.Meeting,
            created.Id,
            $"1:1 with {directReport.FullName} on {created.MeetingDate:MMM d, yyyy}",
            $"Meeting with {directReport.FullName} was created",
            cancellationToken);

        return await MapToDtoAsync(created, cancellationToken);
    }

    public async Task<OneOnOneMeetingDto> UpdateAsync(Guid id, UpdateOneOnOneMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        // Validate the new direct report exists
        var directReport = await _directReportRepository.GetByIdAsync(dto.DirectReportId, cancellationToken);
        if (directReport is null)
        {
            throw new NotFoundException(nameof(DirectReport), dto.DirectReportId);
        }

        entity.Update(dto.DirectReportId, dto.MeetingDate, dto.Location, dto.Agenda);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Updated,
            EntityType.Meeting,
            entity.Id,
            $"1:1 with {directReport.FullName} on {entity.MeetingDate:MMM d, yyyy}",
            $"Meeting with {directReport.FullName} was updated",
            cancellationToken);

        return await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _meetingRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(OneOnOneMeeting), id);
        }

        var directReport = await _directReportRepository.GetByIdAsync(entity.DirectReportId, cancellationToken);

        await _meetingRepository.DeleteAsync(id, cancellationToken);

        await _activityService.LogActivityAsync(
            ActivityType.Deleted,
            EntityType.Meeting,
            id,
            $"1:1 with {directReport?.FullName ?? "Unknown"} on {entity.MeetingDate:MMM d, yyyy}",
            $"Meeting with {directReport?.FullName ?? "Unknown"} was deleted",
            cancellationToken);
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
            MeetingDate = entity.MeetingDate,
            Location = entity.Location,
            Agenda = entity.Agenda,
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

    private static MeetingNoteDto MapNoteToDto(MeetingNote note, DateOnly meetingDate, string directReportName) => new()
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
        NoteCategory.Agenda => "Agenda",
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
