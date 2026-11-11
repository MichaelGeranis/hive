using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteExternalDependencyRepository : IExternalDependencyRepository
{
    private readonly HiveDbContext _context;

    public SqliteExternalDependencyRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ExternalDependency>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.ExternalDependencies
            .AsNoTracking()
            .Where(x => x.InitiativeId == initiativeId)
            .OrderBy(x => x.Team).ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = await _context.Initiatives
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return await _context.ExternalDependencies
            .AsNoTracking()
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .OrderBy(x => x.Team).ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.ExternalDependencies
            .Where(x => x.InitiativeId == initiativeId)
            .ToListAsync(cancellationToken);

        _context.ExternalDependencies.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExternalDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExternalDependencies.FindAsync([id], cancellationToken);
    }

    public async Task UpdateAsync(ExternalDependency entity, CancellationToken cancellationToken = default)
    {
        _context.ExternalDependencies.UpdateUntracked(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExternalDependency> AddAsync(ExternalDependency entity, CancellationToken cancellationToken = default)
    {
        _context.ExternalDependencies.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ExternalDependencies.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.ExternalDependencies.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ExternalDependency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExternalDependencies.AsNoTracking().OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
