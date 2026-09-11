using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteOneOnOneMeetingRepository : IOneOnOneMeetingRepository
{
    private readonly HiveDbContext _context;

    public SqliteOneOnOneMeetingRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OneOnOneMeeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OneOnOneMeetings.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeeting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OneOnOneMeetings
            .OrderByDescending(x => x.MeetingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OneOnOneMeeting>> GetByDirectReportIdAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.OneOnOneMeetings
            .Where(x => x.DirectReportId == directReportId)
            .OrderByDescending(x => x.MeetingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OneOnOneMeeting> Items, int TotalCount)> GetFilteredPagedAsync(
        int skip,
        int take,
        Guid? directReportId,
        bool unlinkedOnly,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OneOnOneMeetings.AsQueryable();

        if (unlinkedOnly)
        {
            query = query.Where(m => m.DirectReportId == null);
        }
        else if (directReportId.HasValue)
        {
            query = query.Where(m => m.DirectReportId == directReportId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.Title.ToLower().Contains(term) ||
                m.Content.ToLower().Contains(term) ||
                m.Tags.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.MeetingDate)
            .ThenByDescending(m => m.UpdatedAt ?? m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<(Guid? DirectReportId, int Count)>> GetCountsByDirectReportAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _context.OneOnOneMeetings
            .GroupBy(m => m.DirectReportId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.Select(c => (c.Key, c.Count)).ToList();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OneOnOneMeetings.CountAsync(cancellationToken);
    }

    public async Task<OneOnOneMeeting> AddAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default)
    {
        _context.OneOnOneMeetings.Add(meeting);
        await _context.SaveChangesAsync(cancellationToken);
        return meeting;
    }

    public async Task UpdateAsync(OneOnOneMeeting meeting, CancellationToken cancellationToken = default)
    {
        _context.OneOnOneMeetings.Update(meeting);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OneOnOneMeetings.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.OneOnOneMeetings.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OneOnOneMeetings.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
