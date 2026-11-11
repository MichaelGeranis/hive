using Hive.Core.Entities;
using Hive.Core.Interfaces;

namespace Hive.Infrastructure.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IExternalDependencyRepository.
/// </summary>
public class ExternalDependencyRepository : IExternalDependencyRepository
{
    private readonly InMemoryDbContext _context;

    public ExternalDependencyRepository(InMemoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<IReadOnlyList<ExternalDependency>> GetByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var entities = _context.ExternalDependencies.Values
            .Where(x => x.InitiativeId == initiativeId)
            .OrderBy(x => x.Team).ThenBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ExternalDependency>>(entities);
    }

    public Task<IReadOnlyList<ExternalDependency>> GetByQuarterAsync(Guid quarterId, CancellationToken cancellationToken = default)
    {
        var quarterInitiativeIds = _context.Initiatives.Values
            .Where(i => i.QuarterId == quarterId)
            .Select(i => i.Id)
            .ToHashSet();

        var entities = _context.ExternalDependencies.Values
            .Where(x => quarterInitiativeIds.Contains(x.InitiativeId))
            .OrderBy(x => x.Team).ThenBy(x => x.CreatedAt)
            .ToList();
        return Task.FromResult<IReadOnlyList<ExternalDependency>>(entities);
    }

    public Task DeleteByInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default)
    {
        foreach (var id in _context.ExternalDependencies.Values.Where(x => x.InitiativeId == initiativeId).Select(x => x.Id).ToList())
        {
            _context.ExternalDependencies.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<ExternalDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ExternalDependencies.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(ExternalDependency entity, CancellationToken cancellationToken = default)
    {
        if (!_context.ExternalDependencies.ContainsKey(entity.Id))
        {
            throw new InvalidOperationException($"ExternalDependency with id '{entity.Id}' not found.");
        }
        _context.ExternalDependencies[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<ExternalDependency> AddAsync(ExternalDependency entity, CancellationToken cancellationToken = default)
    {
        if (!_context.ExternalDependencies.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"ExternalDependency with id '{entity.Id}' already exists.");
        }
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _context.ExternalDependencies.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExternalDependency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExternalDependency> entities = _context.ExternalDependencies.Values.OrderBy(x => x.CreatedAt).ToList();
        return Task.FromResult(entities);
    }
}
