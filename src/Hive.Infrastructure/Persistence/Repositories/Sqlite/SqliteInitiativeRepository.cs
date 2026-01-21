using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteInitiativeRepository : IInitiativeRepository
{
    private readonly HiveDbContext _context;

    public SqliteInitiativeRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Initiative?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Initiative>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Initiative>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives
            .Where(x => x.QuarterId == quarterId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Initiative>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Initiative>> GetByStatusAsync(InitiativeStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Initiative> AddAsync(Initiative initiative, CancellationToken cancellationToken = default)
    {
        _context.Initiatives.Add(initiative);
        await _context.SaveChangesAsync(cancellationToken);
        return initiative;
    }

    public async Task UpdateAsync(Initiative initiative, CancellationToken cancellationToken = default)
    {
        _context.Initiatives.Update(initiative);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Initiatives.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.Initiatives.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Initiatives.AnyAsync(x => x.Id == id, cancellationToken);
    }
}
