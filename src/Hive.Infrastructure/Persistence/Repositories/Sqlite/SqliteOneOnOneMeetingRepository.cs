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
