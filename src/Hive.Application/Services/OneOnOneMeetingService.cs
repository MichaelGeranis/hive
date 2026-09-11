using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;

namespace Hive.Application.Services;

/// <summary>
/// Service implementing use cases for 1:1 meetings. A 1:1 is one markdown note, and the
/// person it belongs to is derived from its tags rather than picked from a list.
/// </summary>
public class OneOnOneMeetingService : IOneOnOneMeetingService
{
    private readonly IOneOnOneMeetingRepository _meetingRepository;
    private readonly IDirectReportRepository _directReportRepository;
    private readonly IActivityService _activityService;

    public OneOnOneMeetingService(
        IOneOnOneMeetingRepository meetingRepository,
        IDirectReportRepository directReportRepository,
        IActivityService activityService)
    {
        _meetingRepository = meetingRepository ?? throw new ArgumentNullException(nameof(meetingRepository));
        _directReportRepository = directReportRepository ?? throw new ArgumentNullException(nameof(directReportRepository));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    public async Task<OneOnOneMeetingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _meetingRepository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var names = await GetReportNamesAsync(cancellationToken);
        return MapToDto(entity, names);
    }

    public async Task<PagedResult<OneOnOneMeetingDto>> GetFilteredPagedAsync(
        MeetingPaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var (entities, totalCount) = await _meetingRepository.GetFilteredPagedAsync(
            pagination.Skip,
            pagination.PageSize,
            pagination.DirectReportId,
            pagination.UnlinkedOnly,
            pagination.SearchTerm,
            cancellationToken);

        var names = await GetReportNamesAsync(cancellationToken);
        var dtos = entities.Select(entity => MapToDto(entity, names)).ToList();

        return PagedResult<OneOnOneMeetingDto>.Create(dtos, totalCount, pagination);
    }

    public async Task<IReadOnlyList<OneOnOneMeetingDto>> GetByDirectReportIdAsync(
        Guid directReportId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _meetingRepository.GetByDirectReportIdAsync(directReportId, cancellationToken);
        var names = await GetReportNamesAsync(cancellationToken);
        return entities.Select(entity => MapToDto(entity, names)).ToList();
    }

    public async Task<IReadOnlyList<MeetingCountDto>> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _meetingRepository.GetCountsByDirectReportAsync(cancellationToken);
        return counts
            .Select(c => new MeetingCountDto { DirectReportId = c.DirectReportId, Count = c.Count })
            .ToList();
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _meetingRepository.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Opens a blank 1:1 note, on today unless another day is given. The manager starts
    /// typing during the meeting; the note exists from the first keystroke.
    /// </summary>
    public async Task<OneOnOneMeetingDto> CreateBlankAsync(CreateBlankMeetingDto dto, CancellationToken cancellationToken = default)
    {
        var meetingDate = dto.MeetingDate ?? DateOnly.FromDateTime(DateTime.Today);
        var entity = new OneOnOneMeeting(meetingDate, content: null, tags: dto.Tags);

        var names = await ResolveAndLinkAsync(entity, cancellationToken);
        var created = await _meetingRepository.AddAsync(entity, cancellationToken);

        await LogAsync(ActivityType.Created, created, names, "was logged", cancellationToken);

        return MapToDto(created, names);
    }

    /// <summary>
    /// Saves the body of a 1:1 as it is written and re-derives its title from the first
    /// line. Deliberately not logged to the activity feed: this runs on every autosave.
    /// </summary>
    public async Task<OneOnOneMeetingDto> UpdateContentAsync(Guid id, UpdateMeetingContentDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateContent(dto.Content);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        var names = await GetReportNamesAsync(cancellationToken);
        return MapToDto(entity, names);
    }

    /// <summary>
    /// Replaces the tags, then re-resolves who the 1:1 is with and when it happened.
    /// The tags are the source of truth; the stored link is derived from them.
    /// </summary>
    public async Task<OneOnOneMeetingDto> UpdateTagsAsync(Guid id, UpdateMeetingTagsDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.UpdateTags(dto.Tags);
        var names = await ResolveAndLinkAsync(entity, cancellationToken);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        return MapToDto(entity, names);
    }

    public async Task<OneOnOneMeetingDto> UpdateDateAsync(Guid id, UpdateMeetingDateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);

        entity.SetMeetingDate(dto.MeetingDate);
        await _meetingRepository.UpdateAsync(entity, cancellationToken);

        var names = await GetReportNamesAsync(cancellationToken);
        return MapToDto(entity, names);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrowAsync(id, cancellationToken);
        var names = await GetReportNamesAsync(cancellationToken);

        await _meetingRepository.DeleteAsync(id, cancellationToken);

        await LogAsync(ActivityType.Deleted, entity, names, "was deleted", cancellationToken);
    }

    /// <summary>
    /// Resolves the person named by the tags and links the 1:1 to them.
    /// A tag matches a report by first name, last name, or the two joined in either
    /// order, compared on letters and digits only. A tag set that names nobody — or more
    /// than one person — leaves the 1:1 unlinked rather than guessing.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, string>> ResolveAndLinkAsync(
        OneOnOneMeeting meeting,
        CancellationToken cancellationToken)
    {
        var reports = await _directReportRepository.GetAllAsync(cancellationToken);
        var matches = new HashSet<Guid>();

        foreach (var tag in meeting.GetTagsList())
        {
            var token = NoteText.NormalizeNameToken(tag);
            if (token.Length == 0)
            {
                continue;
            }

            foreach (var report in reports)
            {
                if (NameTokensFor(report).Contains(token))
                {
                    matches.Add(report.Id);
                }
            }
        }

        meeting.LinkTo(matches.Count == 1 ? matches.Single() : null);

        return reports.ToDictionary(r => r.Id, r => r.FullName);
    }

    /// <summary>
    /// The forms of a person's name a tag is allowed to use.
    /// </summary>
    private static HashSet<string> NameTokensFor(DirectReport report)
    {
        var first = NoteText.NormalizeNameToken(report.FirstName);
        var last = NoteText.NormalizeNameToken(report.LastName);

        return new HashSet<string>(StringComparer.Ordinal)
        {
            first,
            last,
            first + last,
            last + first
        };
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetReportNamesAsync(CancellationToken cancellationToken)
    {
        var reports = await _directReportRepository.GetAllAsync(cancellationToken);
        return reports.ToDictionary(r => r.Id, r => r.FullName);
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

    private async Task LogAsync(
        ActivityType activityType,
        OneOnOneMeeting meeting,
        IReadOnlyDictionary<Guid, string> names,
        string verb,
        CancellationToken cancellationToken)
    {
        var who = meeting.DirectReportId.HasValue && names.TryGetValue(meeting.DirectReportId.Value, out var name)
            ? name
            : "nobody yet";

        await _activityService.LogActivityAsync(
            activityType,
            EntityType.Meeting,
            meeting.Id,
            $"1:1 with {who}",
            $"1:1 with {who} on {meeting.MeetingDate:yyyy-MM-dd} {verb}",
            cancellationToken);
    }

    private static OneOnOneMeetingDto MapToDto(OneOnOneMeeting entity, IReadOnlyDictionary<Guid, string> names)
    {
        string? reportName = null;
        if (entity.DirectReportId.HasValue && names.TryGetValue(entity.DirectReportId.Value, out var name))
        {
            reportName = name;
        }

        return new OneOnOneMeetingDto
        {
            Id = entity.Id,
            DirectReportId = entity.DirectReportId,
            DirectReportName = reportName,
            IsUnlinked = entity.IsUnlinked(),
            MeetingDate = entity.MeetingDate,
            Title = entity.Title,
            Content = entity.Content,
            Tags = entity.Tags,
            TagsList = entity.GetTagsList(),
            Snippet = BuildSnippet(entity.Content),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Builds the one-line preview shown under a 1:1's title in the list, skipping the
    /// line the title itself came from.
    /// </summary>
    private static string BuildSnippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var skippedTitleLine = false;
        var parts = new List<string>();

        foreach (var line in content.Split('\n'))
        {
            var text = line.Replace("\r", string.Empty).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (!skippedTitleLine)
            {
                skippedTitleLine = true;
                continue;
            }

            parts.Add(text);

            if (parts.Sum(p => p.Length) > 200)
            {
                break;
            }
        }

        var snippet = string.Join(" ", parts);
        return snippet.Length > 200 ? snippet[..200].TrimEnd() : snippet;
    }
}
