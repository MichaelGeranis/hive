using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteAllocationRepository : IAllocationRepository
{
    private readonly HiveDbContext _context;

    public SqliteAllocationRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Allocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Allocations.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Allocations.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.Allocations
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByDirectReportAsync(Guid directReportId, CancellationToken cancellationToken = default)
    {
        return await _context.Allocations
            .Where(x => x.DirectReportId == directReportId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetBySprintAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        return await _context.Allocations
            .Where(x => x.SprintId == sprintId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Allocation>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = await _context.Initiatives
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return await _context.Allocations
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Allocation?> GetByInitiativeDirectReportSprintAsync(
        Guid initiativeId,
        Guid directReportId,
        Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Allocations
            .FirstOrDefaultAsync(x =>
                x.InitiativeId == initiativeId &&
                x.DirectReportId == directReportId &&
                x.SprintId == sprintId, cancellationToken);
    }

    public async Task<Allocation> AddAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        _context.Allocations.Add(allocation);
        await _context.SaveChangesAsync(cancellationToken);
        return allocation;
    }

    public async Task UpdateAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        _context.Allocations.Update(allocation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Allocations.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Allocations.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var allocations = await _context.Allocations
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);

        _context.Allocations.RemoveRange(allocations);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
