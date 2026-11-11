using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteQuarterMemberRepository : IQuarterMemberRepository
{
    private readonly HiveDbContext _context;

    public SqliteQuarterMemberRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<QuarterMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.QuarterMembers.FindAsync([id], cancellationToken);
    }

    public async Task UpdateAsync(QuarterMember entity, CancellationToken cancellationToken = default)
    {
        _context.QuarterMembers.UpdateUntracked(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuarterMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        return await _context.QuarterMembers
            .AsNoTracking()
            .Where(x => x.QuarterId == quarterId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuarterMember?> GetByQuarterAndDirectReportAsync(Guid quarterId, Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.QuarterMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.QuarterId == quarterId && x.DirectReportId == directReportId, cancellationToken);
    }

    public async Task DeleteByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.QuarterMembers
            .Where(x => x.QuarterId == quarterId)
            .ToListAsync(cancellationToken);

        _context.QuarterMembers.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuarterMember> AddAsync(QuarterMember entity, CancellationToken cancellationToken = default)
    {
        _context.QuarterMembers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.QuarterMembers.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.QuarterMembers.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<QuarterMember>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.QuarterMembers.AsNoTracking().OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
