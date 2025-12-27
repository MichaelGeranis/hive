using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteMeetingNoteRepository : IMeetingNoteRepository
{
    private readonly HiveDbContext _context;

    public SqliteMeetingNoteRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MeetingNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MeetingNotes.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNote>> GetByMeetingIdAsync(Guid meetingId, bool includePrivate = true, CancellationToken cancellationToken = default)
    {
        var query = _context.MeetingNotes.Where(x => x.MeetingId == meetingId);

        if (!includePrivate)
        {
            query = query.Where(x => !x.IsPrivate);
        }

        return await query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNote>> GetActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MeetingNotes.Where(x => x.Category == NoteCategory.ActionItem);

        if (directReportId.HasValue)
        {
            var meetingIds = await _context.OneOnOneMeetings
                .Where(m => m.DirectReportId == directReportId.Value)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x => meetingIds.Contains(x.MeetingId));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNote>> GetOpenActionItemsAsync(Guid? directReportId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MeetingNotes
            .Where(x => x.Category == NoteCategory.ActionItem
                        && x.ActionStatus != ActionItemStatus.Completed
                        && x.ActionStatus != ActionItemStatus.Cancelled);

        if (directReportId.HasValue)
        {
            var meetingIds = await _context.OneOnOneMeetings
                .Where(m => m.DirectReportId == directReportId.Value)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x => meetingIds.Contains(x.MeetingId));
        }

        return await query
            .OrderBy(x => x.ActionDueDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MeetingNote>> GetOverdueActionItemsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.MeetingNotes
            .Where(x => x.Category == NoteCategory.ActionItem
                        && x.ActionDueDate.HasValue
                        && x.ActionDueDate < now
                        && x.ActionStatus != ActionItemStatus.Completed
                        && x.ActionStatus != ActionItemStatus.Cancelled)
            .OrderBy(x => x.ActionDueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<MeetingNote> AddAsync(MeetingNote note, CancellationToken cancellationToken = default)
    {
        _context.MeetingNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task UpdateAsync(MeetingNote note, CancellationToken cancellationToken = default)
    {
        _context.MeetingNotes.Update(note);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.MeetingNotes.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.MeetingNotes.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MeetingNotes.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
