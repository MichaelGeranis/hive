using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IMeetingNoteRepository.
/// </summary>
public class MeetingNoteRepository : IMeetingNoteRepository
{
    private readonly InMemoryDbContext _context;

    public MeetingNoteRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<MeetingNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.MeetingNotes.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<MeetingNote>> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var entities = _context.MeetingNotes.Values
            .Where(x => x.MeetingId == meetingId)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MeetingNote>>(entities);
    }

    public Task<IReadOnlyList<MeetingNote>> GetActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MeetingNotes.Values
            .Where(x => x.Category == NoteCategory.ActionItem);

        if (directReportId.HasValue)
        {
            var meetingIds = _context.OneOnOneMeetings.Values
                .Where(m => m.DirectReportId == directReportId.Value)
                .Select(m => m.Id)
                .ToHashSet();

            query = query.Where(x => meetingIds.Contains(x.MeetingId));
        }

        var entities = query
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MeetingNote>>(entities);
    }

    public Task<IReadOnlyList<MeetingNote>> GetOpenActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MeetingNotes.Values
            .Where(x => x.Category == NoteCategory.ActionItem
                        && x.ActionStatus != ActionItemStatus.Completed
                        && x.ActionStatus != ActionItemStatus.Cancelled);

        if (directReportId.HasValue)
        {
            var meetingIds = _context.OneOnOneMeetings.Values
                .Where(m => m.DirectReportId == directReportId.Value)
                .Select(m => m.Id)
                .ToHashSet();

            query = query.Where(x => meetingIds.Contains(x.MeetingId));
        }

        var entities = query
            .OrderBy(x => x.ActionDueDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MeetingNote>>(entities);
    }

    public Task<IReadOnlyList<MeetingNote>> GetOverdueActionItemsAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.MeetingNotes.Values
            .Where(x => x.IsOverdue())
            .OrderBy(x => x.ActionDueDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<MeetingNote>>(entities);
    }

    public Task<MeetingNote> AddAsync(MeetingNote note, CancellationToken cancellationToken = default)
    {
        if (!_context.MeetingNotes.TryAdd(note.Id, note))
        {
            throw new InvalidOperationException($"MeetingNote with id '{note.Id}' already exists.");
        }
        return Task.FromResult(note);
    }

    public Task UpdateAsync(MeetingNote note, CancellationToken cancellationToken = default)
    {
        if (!_context.MeetingNotes.ContainsKey(note.Id))
        {
            throw new InvalidOperationException($"MeetingNote with id '{note.Id}' not found.");
        }
        _context.MeetingNotes[note.Id] = note;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.MeetingNotes.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_context.MeetingNotes.ContainsKey(id));
    }
}
