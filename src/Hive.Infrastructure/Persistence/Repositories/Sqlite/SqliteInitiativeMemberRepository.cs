using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteInitiativeMemberRepository : IInitiativeMemberRepository
{
    private readonly HiveDbContext _context;

    public SqliteInitiativeMemberRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InitiativeMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeMembers.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeMember>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeMembers
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeMember>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeMembers
            .Where(x => x.DirectReportId == directReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeMember>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = await _context.Initiatives
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return await _context.InitiativeMembers
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<InitiativeMember> AddAsync(InitiativeMember member, CancellationToken cancellationToken = default)
    {
        _context.InitiativeMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.InitiativeMembers.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.InitiativeMembers.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var members = await _context.InitiativeMembers
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);

        _context.InitiativeMembers.RemoveRange(members);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid initiativeId, Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeMembers
            .AnyAsync(x => x.InitiativeId == initiativeId && x.DirectReportId == directReportId, cancellationToken);
    }
}
