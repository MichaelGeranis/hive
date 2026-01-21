using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hive.Infrastructure.Persistence.Repositories.Sqlite;

public class SqliteInitiativeDependencyRepository : IInitiativeDependencyRepository
{
    private readonly HiveDbContext _context;

    public SqliteInitiativeDependencyRepository(HiveDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InitiativeDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeDependencies.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeDependency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeDependencies.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeDependency>> GetByDependentInitiativeAsync(Guid dependentInitiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeDependencies
            .Where(x => x.DependentInitiativeId == dependentInitiativeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeDependency>> GetByDependencyInitiativeAsync(Guid dependencyInitiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeDependencies
            .Where(x => x.DependencyInitiativeId == dependencyInitiativeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InitiativeDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = await _context.Initiatives
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return await _context.InitiativeDependencies
            .Where(x => quarterInitiativeIds.Contains(x.DependentInitiativeId) ||
                       quarterInitiativeIds.Contains(x.DependencyInitiativeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<InitiativeDependency> AddAsync(InitiativeDependency dependency, CancellationToken cancellationToken = default)
    {
        _context.InitiativeDependencies.Add(dependency);
        await _context.SaveChangesAsync(cancellationToken);
        return dependency;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.InitiativeDependencies.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.InitiativeDependencies.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var dependencies = await _context.InitiativeDependencies
            .Where(x => x.DependentInitiativeId == initiativeId ||
                       x.DependencyInitiativeId == initiativeId)
            .ToListAsync(cancellationToken);

        _context.InitiativeDependencies.RemoveRange(dependencies);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid dependentInitiativeId, Guid dependencyInitiativeId, CancellationToken cancellationToken = default)
    {
        return await _context.InitiativeDependencies
            .AnyAsync(x => x.DependentInitiativeId == dependentInitiativeId &&
                          x.DependencyInitiativeId == dependencyInitiativeId, cancellationToken);
    }
}
