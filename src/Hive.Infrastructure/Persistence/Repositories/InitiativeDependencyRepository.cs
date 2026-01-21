using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IInitiativeDependencyRepository.
/// </summary>
public class InitiativeDependencyRepository : IInitiativeDependencyRepository
{
    private readonly InMemoryDbContext _context;

    public InitiativeDependencyRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<InitiativeDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.InitiativeDependencies.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<InitiativeDependency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeDependencies.Values.ToList();
        return Task.FromResult<IReadOnlyList<InitiativeDependency>>(entities);
    }

    public Task<IReadOnlyList<InitiativeDependency>> GetByDependentInitiativeAsync(Guid dependentInitiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeDependencies.Values
            .Where(x => x.DependentInitiativeId == dependentInitiativeId)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeDependency>>(entities);
    }

    public Task<IReadOnlyList<InitiativeDependency>> GetByDependencyInitiativeAsync(Guid dependencyInitiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.InitiativeDependencies.Values
            .Where(x => x.DependencyInitiativeId == dependencyInitiativeId)
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeDependency>>(entities);
    }

    public Task<IReadOnlyList<InitiativeDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        // Get all initiative IDs for this quarter
        var quarterInitiativeIds = _context.Initiatives.Values
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToHashSet();

        var entities = _context.InitiativeDependencies.Values
            .Where(x => quarterInitiativeIds.Contains(x.DependentInitiativeId) ||
                       quarterInitiativeIds.Contains(x.DependencyInitiativeId))
            .ToList();
        return Task.FromResult<IReadOnlyList<InitiativeDependency>>(entities);
    }

    public Task<InitiativeDependency> AddAsync(InitiativeDependency dependency, CancellationToken cancellationToken = default)
    {
        if (!_context.InitiativeDependencies.TryAdd(dependency.Id, dependency))
        {
            throw new InvalidOperationException($"InitiativeDependency with id '{dependency.Id}' already exists.");
        }
        return Task.FromResult(dependency);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.InitiativeDependencies.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var toRemove = _context.InitiativeDependencies.Values
            .Where(x => x.DependentInitiativeId == initiativeId ||
                       x.DependencyInitiativeId == initiativeId)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _context.InitiativeDependencies.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid dependentInitiativeId, Guid dependencyInitiativeId, CancellationToken cancellationToken = default)
    {
        var exists = _context.InitiativeDependencies.Values
            .Any(x => x.DependentInitiativeId == dependentInitiativeId &&
                     x.DependencyInitiativeId == dependencyInitiativeId);
        return Task.FromResult(exists);
    }
}
